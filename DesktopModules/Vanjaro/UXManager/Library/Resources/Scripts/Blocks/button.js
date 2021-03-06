export default (editor, config = {}) => {
	const c = config;
	let bm = editor.BlockManager;

	if (c.blocks.button) {
		bm.add('button', {
			label: `
				<svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
					<path d="M22,9 C22,8.4 21.5,8 20.75,8 L3.25,8 C2.5,8 2,8.4 2,9 L2,15 C2,15.6 2.5,16 3.25,16 L20.75,16 C21.5,16 22,15.6 22,15 L22,9 Z M21,15 L3,15 L3,9 L21,9 L21,15 Z" fill-rule="nonzero"></path>
						<rect x="4" y="11.5" width="16" height="1"></rect>
				</svg>
				<div class="gjs-block-label">`+ VjLocalized.Button + `</div>
			`,
			category: VjLocalized.Basic,
			content: '<div class="button-box"><a role="button" href="" class="btn btn-primary"><span class="button-text">Button</span></a></div>',
		});
	}

	let domc = editor.DomComponents;
	const defaultType = domc.getType('default');
	const defaultModel = defaultType.model;
	const defaultView = defaultType.view;

	domc.addType('button-box', {
		model: defaultModel.extend({
			defaults: Object.assign({}, defaultModel.prototype.defaults, {
				'custom-name': 'Button Box',
				droppable: false,
				traits: []
			}),
		},
			{
				isComponent(el) {
					if (el && el.classList && el.classList.contains('button-box')) {
						return { type: 'button-box' };
					}
				}
			}),
		view: defaultView
	});

	domc.addType('button', {
		model: defaultModel.extend({
			initToolbar() {
				var model = this;
				if (!model.get('toolbar')) {
					var tb = [];

					tb.push({
						attributes: { class: 'fa fa-arrow-up' },
						command: function (t) {
							return t.runCommand("core:component-exit", {
								force: 1
							})
						}
					});

					if (model.get('draggable')) {
						tb.push({
							attributes: { class: 'fa fa-arrows' },
							command: 'tlb-move',
						});
					}

					if (model.get('copyable')) {
						tb.push({
							attributes: { class: 'fa fa-clone' },
							command: 'vj-copy',
						});
					}

					if (model.get('removable')) {
						tb.push({
							attributes: { class: 'fa fa-trash-o' },
							command: 'vj-delete',
						});
					}

					model.set('toolbar', tb);
				}
			},
			defaults: Object.assign({}, defaultModel.prototype.defaults, {
				droppable: false,
				resizable: {
					tl: 0, // Top left
					tc: 0, // Top center
					tr: 0, // Top right
					cl: 1, // Center left
					cr: 1, // Center right
					bl: 0, // Bottom left
					bc: 0, // Bottom center
					br: 0, // Bottom right
				},
				traits: [
					{
						label: "Style",
						name: "stylee",
						type: 'toggle_radio',
						SwitchClass: true,
						options: [
							{ id: 'fill', name: 'fill', class: '' },
							{ id: 'outline', name: 'outline', class: 'outline-' },
						],
						value: 'fill',
						changeProp: 1,
					}, {
						label: "Size",
						name: "size",
						type: 'toggle_radio',
						SwitchClass: true,
						options: [
							{ id: 'sm', name: 'small', class: 'btn-sm' },
							{ id: 'md', name: 'medium', class: '' },
							{ id: 'lg', name: 'large', class: 'btn-lg' },
						],
						value: 'md',
						changeProp: 1,
					}, {
						label: 'Alignment',
						name: 'alignment',
						type: 'toggle_checkbox',
						UpdateStyles: true,
						options: [
							{ id: 'left', name: 'left', image: 'align-left' },
							{ id: 'center', name: 'center', image: 'align-center' },
							{ id: 'right', name: 'right', image: 'align-right' },
							{ id: 'justify', name: 'justify', image: 'align-justify' },
						],
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
						value: "16",
						changeProp: 1,
					}, {
						label: "Color",
						name: "color",
						type: 'custom_color',
						cssproperties: [
							{ name: "background-color" },
							{ name: "border-color" }
						],
						options: [
							{ id: 'primary', name: 'Primary', class: 'primary' },
							{ id: 'secondary', name: 'Secondary', class: 'secondary' },
							{ id: 'tertiary', name: 'Tertiary', class: 'tertiary' },
							{ id: 'quaternary', name: 'Quaternary', class: 'quaternary' },
							{ id: 'success', name: 'Success', class: 'success' },
							{ id: 'info', name: 'Info', class: 'info' },
							{ id: 'warning', name: 'Warning', class: 'warning' },
							{ id: 'danger', name: 'Danger', class: 'danger' },
							{ id: 'light', name: 'Light', class: 'light' },
							{ id: 'dark', name: 'Dark', class: 'dark' },
						],
						value: 'primary',
						changeProp: 1,
					}, {
						label: " ",
						name: "href",
						type: "href",
						href: "",
						"data_href_type": "url",
					}, {
						label: 'Styles',
						name: 'styles',
						type: 'preset_radio',
						options: [
							{ id: 'normal', name: 'Normal', class: 'normal' },
							{ id: 'button-style-1', name: 'Style 1', class: 'button-style-1' },
							{ id: 'button-style-2', name: 'Style 2', class: 'button-style-2' },
							{ id: 'button-style-3', name: 'Style 3', class: 'button-style-3' },
							{ id: 'button-style-4', name: 'Style 4', class: 'button-style-4' },
							{ id: 'button-style-5', name: 'Style 5', class: 'button-style-5' },
						],
						value: 'Normal',
						changeProp: 1,
					}
				]
			}),
		}, {
				isComponent(el) {
					if (el && el.classList && el.classList.contains('btn')) {
						return { type: 'button' };
					}
				}
			}),
		view: defaultView.extend({
			onRender() {
				var hasClass = this.model.getClasses().find(v => v == 'btn-primary')
				if (typeof hasClass == 'undefined')
					this.model.addClass('btn-primary');
			},
		})
	});

	const textType = domc.getType('text');
	const textModel = textType.model;
	const textView = textType.view;

	domc.addType('button-text', {
		model: textModel.extend({
			defaults: Object.assign({}, textModel.prototype.defaults, {
				'custom-name': 'ButtonText',
				draggable: true,
				droppable: false,
				layerable: false,
				selectable: false,
				hoverable: false,
				traits: [],
			}),
		},
			{
				isComponent(el) {
					if (el && el.classList && el.classList.contains('button-text')) {
						return { type: 'button-text' };
					}
				}
			}),
		view: textView
	});
}
