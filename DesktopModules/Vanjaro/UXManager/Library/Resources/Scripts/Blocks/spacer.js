export default (editor, config = {}) => {
	const c = config;
	let bm = editor.BlockManager;
	let blocks = c.customblocks;
	const cssComposer = editor.CssComposer;

	if (c.blocks.spacer) {
        bm.add('spacer').set({
            label: VjLocalized.Spacer,
			attributes: { class: 'fas fa-chalkboard' },
            category: VjLocalized.Basic,
			content: {
				type: 'spacer',
				content: `<div class="spacer"></div>`,
			}

		});
	}   

	let domc = editor.DomComponents;
	const defaultType = domc.getType('default');
	const defaultModel = defaultType.model;
	const defaultView = defaultType.view;

	domc.addType('spacer', {
		model: defaultModel.extend({
			defaults: Object.assign({}, defaultModel.prototype.defaults, {
				droppable: false,
				//stylable: ['height'],
				classes: ['spacer'],
				resizable: {
					tl: 0, // Top left
					tc: 0, // Top center
					tr: 0, // Top right
					cl: 0, // Center left
					cr: 0, // Center right
					bl: 0, // Bottom left
					bc: 1, // Bottom center
					br: 0, // Bottom right
                    minDim: 10,
					maxDim: 600,
					onMove: function (e) {
						var comp = VjEditor.getSelected();

						//Updating Trait on resize
						if (typeof comp.view != 'undefined') {
							var val = parseInt($(comp.view.el).css('height'));
							comp.getTrait('height').set({
								value: val
							});
				
							if (typeof comp.getTrait('height').el != 'undefined') {
								comp.getTrait('height').el.children[0].value = val
								comp.getTrait('height').el.children[1].value = val
							}
						}
					}
				},
				traits: [
					{
						label: "Space",
						name: "height",
						type: "custom_range",
						cssproperties: [
							{ name: "height" }
						],
						min: "10",
						max: "600",
						value: "50",
                        changeProp: 1,
					}
				]
			})
		}, 
		{
			isComponent(el) {
				if (el && el.classList && el.classList.contains('spacer')) {
					return { type: 'spacer' };
				}
			}
		}),
		view: defaultView
	});
}
