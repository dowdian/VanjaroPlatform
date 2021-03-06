export default (editor, config = {}) => {
	const c = config;
	let bm = editor.BlockManager;
  
	if (c.blocks.link) {
        bm.add('link', {
            label: VjLocalized.Link,
            category: VjLocalized.Basic,
			attributes: { class: 'fas fa-link' },
			content: {
				type: 'link',
				components: [{
					type: "text",
					content: "Link"
				}],
			}
		});
	}

	let domc = editor.DomComponents;
	const linkType = domc.getType('link');
	const linkModel = linkType.model;
	const linkView = linkType.view;

	domc.addType('link', {
		model: linkModel.extend({
			defaults: Object.assign({}, linkModel.prototype.defaults, {
				droppable: true,
				tagName: 'a',
				traits: [
					{
						label: "Alignment",
						type: "toggle_checkbox",
						name: "alignment",
						UpdateStyles: true,
						cssproperties: [{ name: "text-align" }],
						options: [ 
							{ id: 'left', name: 'left', image: 'align-left' },
							{ id: 'center', name: 'center', image: 'align-center' },
							{ id: 'right', name: 'right', image: 'align-right' },
						],
                        value: "none",
                        changeProp: 1,
					}, {
						label: "Color",
						name: "color",
						type: 'custom_color',
						cssproperties: [{ name: "color" }],
						options: [
                            { id: 'primary', name: 'Primary', class: 'text-primary' },
                            { id: 'secondary', name: 'Secondary', class: 'text-secondary' },
                            { id: 'tertiary', name: 'Tertiary', class: 'text-tertiary' },
                            { id: 'quaternary', name: 'Quaternary', class: 'text-quaternary' },
                            { id: 'success', name: 'Success', class: 'text-success' },
                            { id: 'info', name: 'Info', class: 'text-info' },
                            { id: 'warning', name: 'Warning', class: 'text-warning' },
                            { id: 'danger', name: 'Danger', class: 'text-danger' },
                            { id: 'light', name: 'Light', class: 'text-light' },
                            { id: 'dark', name: 'Dark', class: 'text-dark' },
						],
						value: 'primary',
						changeProp: 1,
					}, {
						label: " ",
						name: "href",
						type: "href",
						href: "",
                        "data_href_type": "url",
                        changeProp: 1,
					}
				]
			}),
		}, 
		{
			isComponent(el) {
				if (el && el.tagName && el.tagName.toLowerCase() == 'a') {
					return { type: 'link' };
				}
			}
		}),
		view: linkView.extend({
			onRender() {
				var hasClass = this.model.getClasses().find(v => v == 'link')
				if (typeof hasClass == 'undefined' && this.model.parent().attributes.type != 'blockwrapper')
					this.model.addClass('link');
			},
		})
	});
}
