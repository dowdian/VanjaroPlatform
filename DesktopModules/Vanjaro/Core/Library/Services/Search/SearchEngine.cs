using DotNetNuke.Common.Utilities;
using DotNetNuke.Data;
using DotNetNuke.Entities.Controllers;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.Scheduling;
using DotNetNuke.Services.Search;
using DotNetNuke.Services.Search.Entities;
using DotNetNuke.Services.Search.Internals;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;

namespace Vanjaro.Core.Services.Search
{
    /// -----------------------------------------------------------------------------
    /// Namespace:  DotNetNuke.Services.Search
    /// Project:    DotNetNuke
    /// Class:      SearchEngine
    /// -----------------------------------------------------------------------------
    /// <summary>
    /// The SearchEngine  manages the Indexing of the Portal content
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// -----------------------------------------------------------------------------
    internal class SearchEngine
    {
        internal SearchEngine(ScheduleHistoryItem scheduler, DateTime startTime)
        {
            SchedulerItem = scheduler;
            IndexingStartTime = startTime;
        }

        #region Properties

        public ScheduleHistoryItem SchedulerItem { get; private set; }

        // the time from where to start indexing items
        public DateTime IndexingStartTime { get; private set; }

        #endregion

        #region internal
        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Indexes content within the given time farame
        /// </summary>
        /// -----------------------------------------------------------------------------
        internal void IndexContent()
        {
            //Index TAB META-DATA
            TabIndexer tabIndexer = new TabIndexer();
            int searchDocsCount = GetAndStoreSearchDocuments(tabIndexer);
            int indexedSearchDocumentCount = searchDocsCount;
            AddIdexingResults("Tabs Indexed", searchDocsCount);

            //Index MODULE META-DATA from modules that inherit from ModuleSearchBase
            ModuleIndexer moduleIndexer = new ModuleIndexer(true);
            searchDocsCount = GetAndStoreModuleMetaData(moduleIndexer);
            indexedSearchDocumentCount += searchDocsCount;
            AddIdexingResults("Modules (Metadata) Indexed", searchDocsCount);

            //Index MODULE CONTENT from modules that inherit from ModuleSearchBase
            searchDocsCount = GetAndStoreSearchDocuments(moduleIndexer);
            indexedSearchDocumentCount += searchDocsCount;

            //Index all Defunct ISearchable module content
#pragma warning disable 0618
            SearchItemInfoCollection searchItems = GetContent(moduleIndexer);
            SearchDataStoreProvider.Instance().StoreSearchItems(searchItems);
#pragma warning restore 0618
            indexedSearchDocumentCount += searchItems.Count;

            //Both ModuleSearchBase and ISearchable module content count
            AddIdexingResults("Modules (Content) Indexed", searchDocsCount + searchItems.Count);

            if (!HostController.Instance.GetBoolean("DisableUserCrawling", false))
            {
                //Index User data
                UserIndexer userIndexer = new UserIndexer();
                int userIndexed = GetAndStoreSearchDocuments(userIndexer);
                indexedSearchDocumentCount += userIndexed;
                AddIdexingResults("Users", userIndexed);
            }

            SchedulerItem.AddLogNote("<br/><b>Total Items Indexed: " + indexedSearchDocumentCount + "</b>");
        }

        private void AddIdexingResults(string description, int count)
        {
            SchedulerItem.AddLogNote(string.Format("<br/>&nbsp;&nbsp;{0}: {1}", description, count));
        }

        internal bool CompactSearchIndexIfNeeded(ScheduleHistoryItem scheduleItem)
        {
            ISearchHelper shelper = SearchHelper.Instance;
            if (shelper.GetSearchCompactFlag())
            {
                shelper.SetSearchReindexRequestTime(false);
                System.Diagnostics.Stopwatch stopWatch = System.Diagnostics.Stopwatch.StartNew();
                if (InternalSearchController.Instance.OptimizeSearchIndex())
                {
                    stopWatch.Stop();
                    scheduleItem.AddLogNote(string.Format("<br/><b>Compacted Index, total time {0}</b>", stopWatch.Elapsed));
                }
            }
            return false;
        }

        /// <summary>
        /// Deletes all old documents when re-index was requested, so we start a fresh search.
        /// </summary>
        internal void DeleteOldDocsBeforeReindex()
        {
            IEnumerable<int> portal2Reindex = SearchHelper.Instance.GetPortalsToReindex(IndexingStartTime);
            IInternalSearchController controller = InternalSearchController.Instance;

            foreach (int portalId in portal2Reindex)
            {
                controller.DeleteAllDocuments(portalId, SearchHelper.Instance.GetSearchTypeByName("module").SearchTypeId);
                controller.DeleteAllDocuments(portalId, SearchHelper.Instance.GetSearchTypeByName("tab").SearchTypeId);
            }
        }

        /// <summary>
        /// Deletes all deleted items from the system that are added to deletions table.
        /// </summary>
        internal void DeleteRemovedObjects()
        {
            int deletedCount = 0;
            DateTime cutoffTime = SchedulerItem.StartDate.ToUniversalTime();
            IInternalSearchController searchController = InternalSearchController.Instance;
            DataProvider dataProvider = DataProvider.Instance();
            using (System.Data.IDataReader reader = dataProvider.GetSearchDeletedItems(cutoffTime))
            {
                while (reader.Read())
                {
                    // Note: we saved this in the DB as SearchDocumentToDelete but retrieve as the descendant SearchDocument class
                    SearchDocument document = JsonConvert.DeserializeObject<SearchDocument>(reader["document"] as string);
                    searchController.DeleteSearchDocument(document);
                    deletedCount += 1;
                }
                reader.Close();
            }
            AddIdexingResults("Deleted Objects", deletedCount);
            dataProvider.DeleteProcessedSearchDeletedItems(cutoffTime);
        }

        /// <summary>
        /// Commits (flushes) all added and deleted content to search engine's disk file
        /// </summary>
        internal void Commit()
        {
            InternalSearchController.Instance.Commit();
        }
        #endregion

        #region Private

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Gets all the Search Documents for the given timeframe.
        /// </summary>
        /// <param name="indexer"></param>
        /// -----------------------------------------------------------------------------
        private int GetAndStoreSearchDocuments(IndexingProviderBase indexer)
        {
            IList<SearchDocument> searchDocs;
            System.Collections.ArrayList portals = PortalController.Instance.GetPortals();
            DateTime indexSince;
            int indexedCount = 0;

            foreach (PortalInfo portal in portals.Cast<PortalInfo>())
            {
                indexSince = FixedIndexingStartDate(portal.PortalID);
                try
                {
                    indexedCount += indexer.IndexSearchDocuments(
                        portal.PortalID, SchedulerItem, indexSince, StoreSearchDocuments);
                }
                catch (NotImplementedException)
                {
#pragma warning disable 618
                    searchDocs = indexer.GetSearchDocuments(portal.PortalID, indexSince).ToList();
#pragma warning restore 618
                    StoreSearchDocuments(searchDocs);
                    indexedCount += searchDocs.Count();
                }
            }

            // Include Host Level Items
            indexSince = FixedIndexingStartDate(-1);
            try
            {
                indexedCount += indexer.IndexSearchDocuments(
                    Null.NullInteger, SchedulerItem, indexSince, StoreSearchDocuments);
            }
            catch (NotImplementedException)
            {
#pragma warning disable 618
                searchDocs = indexer.GetSearchDocuments(-1, indexSince).ToList();
#pragma warning restore 618
                StoreSearchDocuments(searchDocs);
                indexedCount += searchDocs.Count();
            }
            return indexedCount;
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Gets all the Searchable Module MetaData SearchDocuments within the timeframe for all portals
        /// </summary>
        /// -----------------------------------------------------------------------------
        private int GetAndStoreModuleMetaData(ModuleIndexer indexer)
        {
            IEnumerable<SearchDocument> searchDocs;
            System.Collections.ArrayList portals = PortalController.Instance.GetPortals();
            DateTime indexSince;
            int indexedCount = 0;
            //DateTime startDate

            foreach (PortalInfo portal in portals.Cast<PortalInfo>())
            {
                indexSince = FixedIndexingStartDate(portal.PortalID);
                searchDocs = indexer.GetModuleMetaData(portal.PortalID, indexSince);
                StoreSearchDocuments(searchDocs);
                indexedCount += searchDocs.Count();
            }

            // Include Host Level Items
            indexSince = FixedIndexingStartDate(Null.NullInteger);
            searchDocs = indexer.GetModuleMetaData(Null.NullInteger, indexSince);
            StoreSearchDocuments(searchDocs);
            indexedCount += searchDocs.Count();

            return indexedCount;
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Ensures all SearchDocuments have a SearchTypeId
        /// </summary>
        /// <param name="searchDocs"></param>
        /// -----------------------------------------------------------------------------
        private static void StoreSearchDocuments(IEnumerable<SearchDocument> searchDocs)
        {
            int defaultSearchTypeId = SearchHelper.Instance.GetSearchTypeByName("module").SearchTypeId;

            IList<SearchDocument> searchDocumentList = searchDocs as IList<SearchDocument> ?? searchDocs.ToList();
            foreach (SearchDocument searchDocument in searchDocumentList.Where(searchDocument => searchDocument.SearchTypeId <= 0))
            {
                searchDocument.SearchTypeId = defaultSearchTypeId;
            }

            InternalSearchController.Instance.AddSearchDocuments(searchDocumentList);
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Adjusts the re-index date/time to account for the portal reindex value
        /// </summary>
        /// -----------------------------------------------------------------------------
        private DateTime FixedIndexingStartDate(int portalId)
        {
            DateTime startDate = IndexingStartTime;
            if (startDate < SqlDateTime.MinValue.Value ||
                SearchHelper.Instance.IsReindexRequested(portalId, startDate))
            {
                return SqlDateTime.MinValue.Value.AddDays(1);
            }
            return startDate;
        }

        #endregion

        #region Obsoleted Methods

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// LEGACY: Deprecated in DNN 7.1. Use 'IndexSearchDocuments' instead.
        /// Used for Legacy Search (ISearchable) 
        /// 
        /// GetContent gets all the content and passes it to the Indexer
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="indexer">The Index Provider that will index the content of the portal</param>
        /// -----------------------------------------------------------------------------
        [Obsolete("Legacy Search (ISearchable) -- Deprecated in DNN 7.1. Use 'IndexSearchDocuments' instead.. Scheduled removal in v10.0.0.")]
        protected SearchItemInfoCollection GetContent(IndexingProviderBase indexer)
        {
            SearchItemInfoCollection searchItems = new SearchItemInfoCollection();
            System.Collections.ArrayList portals = PortalController.Instance.GetPortals();
            for (int index = 0; index <= portals.Count - 1; index++)
            {
                PortalInfo portal = (PortalInfo)portals[index];
                searchItems.AddRange(indexer.GetSearchIndexItems(portal.PortalID));
            }
            return searchItems;
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// LEGACY: Deprecated in DNN 7.1. Use 'IndexSearchDocuments' instead.
        /// Used for Legacy Search (ISearchable) 
        /// 
        /// GetContent gets the Portal's content and passes it to the Indexer
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="portalId">The Id of the Portal</param>
        /// <param name="indexer">The Index Provider that will index the content of the portal</param>
        /// -----------------------------------------------------------------------------
        [Obsolete("Legacy Search (ISearchable) -- Deprecated in DNN 7.1. Use 'IndexSearchDocuments' instead.. Scheduled removal in v10.0.0.")]
        protected SearchItemInfoCollection GetContent(int portalId, IndexingProvider indexer)
        {
            SearchItemInfoCollection searchItems = new SearchItemInfoCollection();
            searchItems.AddRange(indexer.GetSearchIndexItems(portalId));
            return searchItems;
        }

        #endregion

    }
}