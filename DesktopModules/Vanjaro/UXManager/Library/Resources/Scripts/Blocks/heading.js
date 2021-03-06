export default (editor, config = {}) => {
	const c = config;
	let bm = editor.BlockManager;
  
	if (c.blocks.heading) {

        bm.add('heading', {
            label: VjLocalized.Heading,
            category: VjLocalized.Basic,
			attributes: { class: 'fas fa-heading' },
			content: {
				type: 'heading',
				content: 'Heading',
			}
		});
	}

	let domc = editor.DomComponents;
	const textType = domc.getType('text');
	const textModel = textType.model;
	const textView = textType.view;

	domc.addType('heading', {
		model: textModel.extend({
			defaults: Object.assign({}, textModel.prototype.defaults, {
				'custom-name': 'Heading',
				droppable: false,
				tagName: 'h1',
                classes: ['vj-heading', 'text-primary'],
				traits: [
					{
						label: 'Importance',
						name: 'importance',
						type: 'toggle_radio',
						UpdateStyles: true,
						options: [
                            { id: 'H1', name: 'H1' },
                            { id: 'H2', name: 'H2' },
                            { id: 'H3', name: 'H3' },
                            { id: 'H4', name: 'H4' },
                            { id: 'H5', name: 'H5' },
                            { id: 'H6', name: 'H6' },
						],
                        value: 'H1',
                        changeProp: 1,
                    }, {
						label: 'Alignment',
						name: 'alignment',
						type: 'toggle_checkbox',
						UpdateStyles: true,
						cssproperties: [{ name: "text-align" }],
						options: [ 
							{ id: 'left', name: 'left', image: 'align-left' },
							{ id: 'center', name: 'center', image: 'align-center' },
							{ id: 'right', name: 'right', image: 'align-right' },
						],
                        value: 'left',
                        changeProp: 1,
                    }, {
						label: "Font Size",
						name: "fontsize",
						type: "custom_range",
						cssproperties: [
							{ name: "font-size" }
						],
						unitOptions: true,
						units: [
                            { name: 'px' },
                            { name: '%' },
                            { name: 'em' }
						],
						unit: "px",
						min: "10",
						max: "100",
						value: "32",
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
					},
					{
						label: 'Styles',
						name: 'styles',
						type: 'preset_radio',
						options: [
							{ id: 'normal', name: 'Normal', class: 'normal' },
							{ id: 'head-style-1', name: 'Style 1', class: 'head-style-1' },
							{ id: 'head-style-2', name: 'Style 2', class: 'head-style-2' },
							{ id: 'head-style-3', name: 'Style 3', class: 'head-style-3' },
							{ id: 'head-style-4', name: 'Style 4', class: 'head-style-4' },
							{ id: 'head-style-5', name: 'Style 5', class: 'head-style-5' },
							{ id: 'head-style-6', name: 'Style 6', class: 'head-style-6' },
							{ id: 'head-style-7', name: 'Style 7', class: 'head-style-7' },
							{ id: 'head-style-8', name: 'Style 8', class: 'head-style-8' },
							{ id: 'head-style-9', name: 'Style 9', class: 'head-style-9' },
							{ id: 'head-style-10', name: 'Style 10', class: 'head-style-10' },
						],
                        value: 'Normal',
                        changeProp: 1,
					}
				]
			}),
            init() {
            	this.listenTo(this, 'change:importance', this.handleTypeChange);
			},
			handleTypeChange() {
				if (typeof this.attributes.importance != 'undefined' && this.attributes.importance != "") {
					this.attributes.tagName = this.attributes.importance;
					this.view.reset();
				}
			}, 
		}, 
		{
			isComponent(el) {
				if (el && ['H1', 'H2', 'H3', 'H4', 'H5', 'H6', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6'].includes(el.tagName)) {
					return { type: 'heading' };
				}
			}
            }),
        view: textView.extend({
            onRender() {
                var hasClass = this.model.getClasses().find(v => v == 'vj-heading' || v == 'text-primary')
                if (typeof hasClass == 'undefined')
                    this.model.addClass('vj-heading text-primary');
            },
        })
	});
}
