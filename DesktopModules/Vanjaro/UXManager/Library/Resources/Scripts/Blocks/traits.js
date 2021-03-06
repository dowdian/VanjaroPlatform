import _ from 'underscore';
import Grapick from 'grapick';
export default (editor, config = {}) => {

	const tm = editor.TraitManager;
	const cmd = editor.Commands;

	cmd.add('showModal', {
		run(editor, sender) {
		},
	});

	cmd.add('vj-copy', editor => {

		const selected = VjEditor.getSelected();
		const selectedType = selected.attributes.type;

		if (selected.parent() && selected.parent().attributes.type != 'wrapper' && (selectedType == 'button' || selectedType == 'icon' || selectedType == 'list' || selectedType == 'image-gallery-item')) {
			var parent = selected.parent().clone();
			var mainparent = selected.parent().parent();
			mainparent.append(parent);
		}
		else if (selectedType == 'image' && selected.parent() && selected.parent().attributes.type != 'wrapper') {
			var parent = selected.parent().parent().clone();
			var mainparent = selected.parent().parent().parent();
			mainparent.append(parent);
		}

	});

	cmd.add('vj-delete', {
		run(editor, sender, opts = {}) {

			const selected = VjEditor.getSelected() || opts.target;

			VjEditor.select();

			const selectedType = selected.attributes.type;
			const selectedParentType = selected.parent().attributes.type;

			if ((selectedType == 'button' && selectedParentType == 'button-box') || (selectedType == 'icon' && selectedParentType == 'icon-box') || (selectedType == 'list' && selectedParentType == 'list-box') || (selectedType == "image-gallery-item" && selected.parent().attributes.tagName == "picture")) {
				selected.parent().remove();
			}
			else if (selectedType == 'image' && typeof selected.parent().parent() != 'undefined' && selected.parent().parent().attributes.type == 'image-box') {
				selected.parent().parent().remove();
			}
			else if (selectedType == 'column' && selectedParentType == 'row' && selected.parent().parent().attributes.type == 'grid') {
				if (!selected.parent().components().length)
					selected.parent().parent().remove();
				else
					selected.remove();
			}
		}
	});

	//Changing Classes
	global.SwitchClass = function (elInput, component, event) {

		var trait = component.getTrait(event.target.name);
		var comp = component.attributes.type;
		var classes = [];
		var className = '';
		var addClass = true;

		//Removing Custom Color in Inline Style
		if (event.target.name == 'color') {
			var property = component.getTrait('color').attributes.cssproperties;
			$(property).each(function (index, value) {
				component.removeStyle(value.name);
			});
		}

		if (event.target.name == 'background' && component.getTrait("background").getInitValue() != 'gradient') {
			var style = component.getStyle();
			style["background-image"] = "none";
			style["background-color"] = "transparent";
			component.setStyle(style);

			$(component.getEl()).removeClass(function (index, className) {
				component.removeClass((className.match(/\bbg-\S+/g) || []).join(' '));
				return (className.match(/\bbg-\S+/g) || []).join(' ');
			});
		}

		if (event.target.type == 'checkbox' && !$(event.target.parentElement).find("input:checked").length)
			addClass = false;

		if (comp == 'button' && (event.target.name == 'stylee' || event.target.name == 'color')) {

			if (event.target.id == 'outline') {
				var style = component.getStyle();
				style["color"] = component.getTrait("color").getInitValue();
				component.setStyle(style);
				component.removeStyle("background-color");
				$(component.getTrait('styles').el).find('label').css({
					"color": component.getTrait("color").getInitValue(),
					"background-color": ""
				});
			}
			else if (event.target.id == 'fill') {
				var style = component.getStyle();
				style["background-color"] = component.getTrait("color").getInitValue();
				component.setStyle(style);
				component.removeStyle("color");
				$(component.getTrait('styles').el).find('label').css({
					"background-color": component.getTrait("color").getInitValue(),
					"color": ""
				});;
			}
			else {
				component.removeStyle("background-color");
				component.removeStyle("border-color");
				component.removeStyle("color");

				$(component.getTrait('styles').el).find('label').css({
					"background-color": "",
					"border-color": "",
					"color": ""
				});
			}

			var btnStart = 'btn-';
			var btnEnd = '';

			if (event.target.name == 'stylee')
				btnEnd += component.getTrait("color").getInitValue();
			else if (event.target.name == 'color' && component.getTrait("stylee").getInitValue() == 'outline')
				btnStart += 'outline-';

			var colorOpts = component.getTrait('color').attributes.options;

			for (let i = 0; i < colorOpts.length; i++) {
				classes.push(btnStart + 'outline-' + colorOpts[i].class);
				classes.push(btnStart + colorOpts[i].class);
			}

			className = btnStart + trait.attributes.options.find(opt => opt.id == event.target.id).class + btnEnd;
		}
		else {
			classes = trait.attributes.options.map(opt => opt.class);
			className = trait.attributes.options.find(opt => opt.id == event.target.id).class;
		}

		trait.setTargetValue(event.target.id);

		for (let i = 0; i < classes.length; i++) {
			if (classes[i].length > 0) {
				var classes_i_a = classes[i].split(' ');
				for (let j = 0; j < classes_i_a.length; j++) {
					if (classes_i_a[j].length > 0) {
						if (comp == 'grid' && event.target.name == 'horizontalalignment')
							component.components().models[0].removeClass(classes_i_a[j]);
						else if (comp == 'button' && (event.target.name == 'color' || event.target.name == 'stylee' || event.target.name == 'size')) {
							component.removeClass(classes_i_a[j]);
							$(component.getTrait('styles').el).find('label').removeClass(classes_i_a[j]);
						}
						else
							component.removeClass(classes_i_a[j]);
					}
				}
			}
		}

		if (addClass && className != undefined && className != 'GJS_NO_CLASS') {
			const value_a = className.split(' ');
			for (let i = 0; i < value_a.length; i++) {
				if (comp == 'grid' && event.target.name == 'horizontalalignment')
					component.components().models[0].addClass(value_a[i]);
				else if (comp == 'button' && (event.target.name == 'color' || event.target.name == 'stylee' || event.target.name == 'size')) {
					component.addClass(value_a[i]);
					$(component.getTrait('styles').el).find('label').addClass(value_a[i]);
				}
				else
					component.addClass(value_a[i]);
			}
		}
	};

	//Color Picker
	global.CustomColor = function () {
		var comp = VjEditor.getSelected();
		var property = comp.getTrait($(event.target).parents(".colorPicker").attr("id")).attributes.cssproperties;
		var color = comp.getTrait($(event.target).parents(".colorPicker").attr("id")).getInitValue();
		var style = comp.getStyle();
		var trait = comp.getTrait($(event.target).parents(".colorPicker").attr("id"));
		var classes = [];

		if (comp.attributes.type == "button" && (comp.getTrait("stylee").getInitValue() == "outline" || comp.getTrait("stylee").getInitValue() == "fill")) {

			var colorOpts = comp.getTrait('color').attributes.options;

			for (let i = 0; i < colorOpts.length; i++) {
				classes.push('btn-outline-' + colorOpts[i].class);
				classes.push('btn-' + colorOpts[i].class);
			}

			if (comp.getTrait("stylee").getInitValue() == "outline") {
				style["border-color"] = color + " !important";
				style["color"] = color + " !important";
			}
			else if (comp.getTrait("stylee").getInitValue() == "fill") {
				$(property).each(function (index, value) {
					style[value.name] = color + " !important";
				});
			}
		}
		else {
			$(property).each(function (index, value) {
				style[value.name] = color + " !important";
			});
		}

		comp.setStyle(style);

		for (let i = 0; i < classes.length; i++) {
			if (classes[i].length > 0) {
				var classes_i_a = classes[i].split(' ');
				for (let j = 0; j < classes_i_a.length; j++) {
					if (classes_i_a[j].length > 0) {
						comp.removeClass(classes_i_a[j]);
					}
				}
			}
		}
	};

	//Component Styling
	global.UpdateStyles = function (elInput, component, event) {

		var trait = component.getTrait(event.target.name);
		var componentType = component.attributes.type;

		if (componentType != 'image-gallery-item' && !event.target.classList.contains('choose-unit')) {
			if (componentType == 'carousel' && event.target.type == 'radio')
				trait.setTargetValue(event.target.id);
			else
				trait.setTargetValue(event.target.value);
		}

		var property = trait.attributes.cssproperties;
		var style = component.getStyle();
		var val = event.target.value;
		var unit = "px";

		if (trait.attributes.type == "custom_range") {

			var inputRange = elInput.querySelector('.range-wrapper .range');
			var inputNumber = elInput.querySelector('.range-wrapper .number');

			if (componentType == 'divider' || componentType == 'heading' || componentType == 'text' || componentType == 'button' || componentType == 'list') {

				if (event.target.name == "width" || event.target.name == "fontsize") {
					unit = $(".unit-wrapper input.choose-unit:checked").val();
					component.set({ 'unit': unit });
				}

				if (event.target.classList.contains('choose-unit')) {

					if (unit == "px") {
						if (componentType == 'divider') {
							$(inputRange).attr("max", "1920");
							style["width"] = "100px";
							inputNumber.value = inputRange.value = "100";
							trait.setTargetValue('100');
						}
						else if (componentType == 'heading') {
							$(inputRange).attr({ "min": "10", "max": "100", "step": "1" });
							style["font-size"] = "32px";
							inputNumber.value = inputRange.value = "32";
							trait.setTargetValue('32');
						}
						else if (componentType == 'text' || componentType == 'button' || componentType == 'list') {
							$(inputRange).attr({ "min": "10", "max": "100", "step": "1" });
							style["font-size"] = "16px";
							inputNumber.value = inputRange.value = "16";
							trait.setTargetValue('16');
						}
					}
					else if (unit == "%") {
						$(inputRange).attr({ "min": "0", "max": "100", "step": "1" });
						style["font-size"] = "100%";
						inputNumber.value = inputRange.value = "100";
						trait.setTargetValue('100');
					}
					else if (unit == "em") {
						$(inputRange).attr({ "min": ".1", "max": "10", "step": ".1" });
						style["font-size"] = "1em";
						inputNumber.value = inputRange.value = "1";
						trait.setTargetValue('1');
					}
				}
				else {
					$(property).each(function (index, value) {
						style[value.name] = val + unit;
					});
				}
			}
			else {
				$(property).each(function (index, value) {
					style[value.name] = val + unit;
				});
			}

			if (event.target.className == "range")
				inputNumber.value = inputRange.value;
			else if (event.target.className == "number")
				inputRange.value = inputNumber.value;

			if (componentType == 'image-gallery-item' && (event.target.name == "width" || event.target.name == "height")) {

				var width = "";
				var height = "";

				if (event.target.name == 'width') {
					width = inputNumber.value;
					height = component.getTrait('height').getInitValue();
				}
				else {
					width = component.getTrait('width').getInitValue();
					height = inputNumber.value;
				}

				$(component.parent().components().models).each(function () {
					var traitGalleryItem = this.getTrait(event.target.name);
					traitGalleryItem.set({
						'value': event.target.value
					});

					this.setStyle('width:' + width + '; height:' + height + ';');
				});
			}
		}
		else {

			unit = ""

			$(property).each(function (index, value) {
				style[value.name] = val + unit;
			});
		}

		if (event.target.name == "alignment") {

			if ($(event.target.parentElement).find("input:checked").length) {

				component.set({ 'alignment': val });

				if (componentType == 'icon' || componentType == 'list') {
					component.parent().setStyle('text-align:' + val + '; display: block;');
				}
				else if (componentType == 'image') {
					component.parent().parent().setStyle('text-align:' + val + '; display: block;');
				}
				else if (componentType == 'button') {
					component.parent().setStyle('text-align:' + val + '; display: block;');

					if (style.width != "100%")
						component.set({ 'width': component.getStyle()["width"] });

					if (event.target.value == "justify") {
						style["width"] = "100%";
						style["display"] = "block";
						component.set({ 'resizable': false });

						$(component.getTrait('styles').el).find('label').css('width', '100%');
					}
					else {
						style["width"] = component.attributes["width"];
						style["display"] = "inline-block";
						component.set({
							'resizable': {
								tl: 0, // Top left
								tc: 0, // Top center
								tr: 0, // Top right
								cl: 1, // Center left
								cr: 1, // Center right
								bl: 0, // Bottom left
								bc: 0, // Bottom center
								br: 0, // Bottom right
							}
						});

						$(component.getTrait('styles').el).css('text-align', val);

						if (typeof component.attributes["width"] != "undefined")
							$(component.getTrait('styles').el).find('label').css('width', component.attributes["width"]);
						else
							$(component.getTrait('styles').el).find('label').css('width', 'auto');

					}
				}
				else if (componentType == 'divider') {
					style["display"] = "table";

					if (event.target.value == "left") {
						style["margin-left"] = "0";
						style["margin-right"] = "auto";
					}
					else if (event.target.value == "center") {
						style["margin-left"] = "auto";
						style["margin-right"] = "auto";
					}
					else if (event.target.value == "right") {
						style["margin-left"] = "auto";
						style["margin-right"] = "0";
					}
				}
				else {
					style["text-align"] = val;
					style["display"] = "block";
				}
			}
			else {

				component.set({ 'alignment': 'none' });

				if (componentType == 'icon' || componentType == 'list') {
					component.parent().setStyle('display: inline-block;');
				}
				else if (componentType == 'image') {
					component.parent().parent().setStyle('display: inline-block;');
				}
				else if (componentType == 'button') {
					style["width"] = component.attributes["width"];
					component.parent().setStyle('display: inline-block;');
					component.set({
						'resizable': {
							tl: 0, // Top left
							tc: 0, // Top center
							tr: 0, // Top right
							cl: 1, // Center left
							cr: 1, // Center right
							bl: 0, // Bottom left
							bc: 0, // Bottom center
							br: 0, // Bottom right
						}
					});
				}
				else {
					style["display"] = "inline-block";
					style["text-align"] = "unset";
				}
			}
		}

		if (event.target.name == "frame") {

			if (event.target.value == "none") {
				style["border-width"] = "0";
				style["padding"] = "10px";
				style["border-radius"] = "0";
			}
			else if (event.target.value == "circle") {
				style["border-width"] = component.getTrait('framewidth').getInitValue() + "px";
				style["border-radius"] = "50%";
			}
			else if (event.target.value == "square") {
				style["border-width"] = component.getTrait('framewidth').getInitValue() + "px";
				style["border-radius"] = "0";
			}

		}

		if (componentType == 'list' && event.target.name == 'ul_list_style') {
			if (event.target.value == 'none')
				style['padding-left'] = '0';
			else
				style['padding-left'] = '40px';
		}

		if (componentType == 'grid')
			component.components().models[0].setStyle(style);
		else if (componentType != 'image-gallery-item')
			component.setStyle(style);
	};

	tm.addType('textarea', {
		createInput({ trait }) {

			const el = document.createElement('div');
			var textarea = document.createElement('textarea');

			textarea.setAttribute("name", trait.attributes.name);
			textarea.setAttribute("id", trait.attributes.name);
			textarea.setAttribute("cols", "42");
			textarea.setAttribute("rows", "3");

			el.appendChild(textarea);

			var textarea = el.getElementsByTagName('textarea');
			var traitValue = trait.getInitValue();

			$(textarea).val(traitValue);

			return el;
		},
		eventCapture: ['input'],
		debounce: function (func, wait, immediate) {
			var timeout;
			return function () {
				var context = this, args = arguments;
				var later = function () {
					timeout = null;
					if (!immediate) func.apply(context, args);
				};
				var callNow = immediate && !timeout;
				clearTimeout(timeout);
				timeout = setTimeout(later, wait);
				if (callNow) func.apply(context, args);
			};
		},
		onEvent({ elInput, component, event }) {
			if (event.type == 'change') {
				var caption = elInput.querySelector('textarea').value;
				component.addAttributes({ alt: caption });
				component.getTrait('caption').setTargetValue(caption);
			}
		}
	});

	//Link 
	tm.addType('href', {
		createInput({ trait }) {
			const el = document.createElement('div');
			el.classList.add("link-wrapper");
			el.id = trait.attributes.name;
			el.innerHTML = `
				<hr />
				<div class="option-block">
					<input type="radio" id="URL" name="LinkType" data-type="url">
					<label for="URL"><em class="fas fa-link"></em></label>
					<input type="radio" id="Page" name="LinkType" data-type="page">
					<label for="Page"><em class="far fa-file"></em></label>			
					<input type="radio" id="Mail" name="LinkType" data-type="email">
					<label for="Mail"><em class="far fa-envelope"></em></label>		
					<input type="radio" id="Phone" name="LinkType" data-type="phone">
					<label for="Phone"><em class="fas fa-phone-alt"></em></label>	
				</div>
				<div class="link-block">
					<div id="url" class="link-type">
						<input type="url" id="vj_link_target" class="url" placeholder="Insert URL" ondblclick="window.parent.OpenPopUp(null, 900, 'right', 'Link', window.parent.CurrentExtTabUrl + '&guid=a0a86356-6c9f-49c4-a431-24917641cb32');" autofocus/>
                        <em class="fas fa-link urlbtn" onclick="window.parent.OpenPopUp(null, 900, 'right', 'Link', window.parent.CurrentExtTabUrl + '&guid=a0a86356-6c9f-49c4-a431-24917641cb32');"></em>
					</div>
					<div id="page" class="link-type">
						<select class="page vj_lnkpage">							
						</select>
					</div>
					<div id="email" class="link-type">
						<input type="email" autofocus class="email" placeholder="Insert email"/>
						<input type="text" class="subject" placeholder="Insert subject"/>
					</div>
					<div id="phone" class="link-type">
						<input type="tel" autofocus class="phone" placeholder="Insert phone number"/>
					</div>
				</div>
				<div class="target-block">
					<div class="title">Open in new tab</div>
					<div class="toggle-blocks">
						<input type="radio" id="yes" name="target">
						<label class="tab-type" for="yes">Yes</label>
						<input type="radio" id="no" name="target">
						<label class="tab-type" for="no">No</label>
					</div>
				</div>
			`;

			var pid = trait.target.attributes.pid;
			var href = trait.attributes.href || trait.target.attributes.href;
			var options = el.querySelector('.option-block');
			var wrapper = el.querySelector('.link-block');
			var target = el.querySelector('.target-block');

			$(el).find(".link-type").hide();

			if (typeof href != 'undefined') {

				if (href.indexOf('http') >= 0) {
					$(options).find("#URL").prop('checked', true);
					$(wrapper).find("#url").show();
					$(wrapper).find("#url input").val(href);
				}
				else if (href.indexOf('mailto') >= 0) {
					$(options).find("#Mail").prop('checked', true);
					$(wrapper).find("#email").show();
					$(target).hide();

					if (href.indexOf('?subject') || href.indexOf('&subject')) {

						var email = '';
						var subject = '';

						if (href.indexOf('?subject') >= 0) {
							email = href.substr(0, href.indexOf('?subject'));
							subject = href.substr(href.indexOf('?subject')).replace('?subject=', '');
						}
						else if (href.indexOf('&subject') >= 0 && href.indexOf('?') >= 0) {
							email = href.substr(0, href.indexOf('?'));
							subject = href.substr(href.indexOf('&subject')).replace('&subject=', '');
						}
						else if (href.indexOf('&subject') >= 0) {
							email = href.substr(0, href.indexOf('&subject'));
							subject = href.substr(href.indexOf('&subject')).replace('&subject=', '')
						}

						$(wrapper).find("#email input.email").val(email.replace('mailto:', ''));
						$(wrapper).find("#email input.subject").val(subject);
					}
					else
						$(wrapper).find("#email input.email").val(href.replace('mailto:', ''));
				}
				else if (href.indexOf('tel') >= 0) {
					$(options).find("#Phone").prop('checked', true);
					$(wrapper).find("#phone").show();
					$(target).hide();
					$(wrapper).find("#phone input").val(href.replace('tel:', ''));
				}
				else {
					$(options).find("#Page").prop('checked', true);
					$(wrapper).find("#page").show();
					this.loadPages('page', pid);
				}
			}
			else {
				$(options).find("#URL").prop('checked', true);
				$(wrapper).find("#url").show();
				$(wrapper).find("#url input").val(href);
			}

			if (typeof trait.target.getAttributes().target == 'undefined')
				$(target).find("input#no").prop('checked', true);
			else
				$(target).find("input#yes").prop('checked', true);

			return el;
		},
		eventCapture: ['input', 'select'],
		debounce: function (func, wait, immediate) {
			var timeout;
			return function () {
				var context = this, args = arguments;
				var later = function () {
					timeout = null;
					if (!immediate) func.apply(context, args);
				};
				var callNow = immediate && !timeout;
				clearTimeout(timeout);
				timeout = setTimeout(later, wait);
				if (callNow) func.apply(context, args);
			};
		},
		loadPages: function (val, pid) {
			if (val == "page" && $('.vj_lnkpage>option').length == 0) {
				var sf = $.ServicesFramework(-1);
				$.ajax({
					type: "GET",
					url: window.location.origin + $.ServicesFramework(-1).getServiceRoot("Vanjaro") + "Page/GetPages",
					headers: {
						'ModuleId': parseInt(sf.getModuleId()),
						'TabId': parseInt(sf.getTabId()),
						'RequestVerificationToken': sf.getAntiForgeryValue()
					},
					success: function (response) {
						$('.vj_lnkpage').html('');
						$.each(response, function (k, v) {
							if (pid == v.Value) {
								$('.vj_lnkpage').append("<option pid='" + v.Value + "' value='" + v.Url + "' selected='selected'>" + v.Text + "</option>");
							}
							else
								$('.vj_lnkpage').append("<option pid='" + v.Value + "' value='" + v.Url + "'>" + v.Text + "</option>");
						});
					}
				});
			}
		},
		onEvent({ elInput, component, event }) {

			var href = '';

			function setURL() {
				component.set({ href: href });

				if (component.attributes.type == 'carousel-image')
					component.parent().parent().addAttributes({ href: href });
				else
					component.addAttributes({ href: href });
			}

			function changeURL() {

				var val = $(".link-wrapper .option-block").find("input:checked").attr("data-type");

				switch (val) {
					case 'url':
						href = elInput.querySelector('.url').value;
						break;
					case 'page':
						var pid = $(elInput.querySelector('.page')).children("option:selected").attr('pid');
						if (pid) {
							var sf = $.ServicesFramework(-1);
							$.ajax({
								type: "GET",
								url: window.location.origin + $.ServicesFramework(-1).getServiceRoot("Vanjaro") + "Page/GetPageUrl?TabID=" + parseInt(pid),
								headers: {
									'ModuleId': parseInt(sf.getModuleId()),
									'TabId': parseInt(sf.getTabId()),
									'RequestVerificationToken': sf.getAntiForgeryValue()
								},
								success: function (response) {
									var valPage = response;

									href = `${valPage}`;
									component.set({ 'pid': pid });

									if (typeof href != '')
										setURL();
								}
							});
						}
						break;
					case 'email':
						var ID = elInput.querySelector('.email').value;
						var Sub = elInput.querySelector('.subject').value;
						href = `mailto:${ID}${Sub ? `?subject=${Sub}` : ''}`;
						break;
					case 'phone':
						var num = elInput.querySelector('.phone').value;
						href = `tel:${num}`;
						break;
				}

			}

			changeURL();

			if (event.target.name == "LinkType") {

				var val = event.target.getAttribute('data-type');

				$(".link-wrapper .link-block .link-type").hide();
				$(".link-wrapper .link-block").find("#" + val).show();

				$(".link-wrapper .link-block .link-type").find("input:visible:first").focus();
				$(".link-wrapper .link-block").find("select").focus();

				if (val == "email" || val == "phone")
					$(".link-wrapper").find(".target-block").hide();
				else {
					$(".link-wrapper").find(".target-block").show();
					this.loadPages(val, 0);
				}

				setURL();
			}

			if (href != '') {

				var UXManager_Search = this.debounce(function () {
					setURL();
				}, 500);

				$(".link-wrapper").find("input").off('keyup').on('keyup', UXManager_Search);
			}

			if (event.target.name == "target") {
				if (event.target.id == "yes") {
					component.addAttributes({ 'target': '_blank', 'rel': 'noopener' });
				}
				else {
					const attr = component.getAttributes();
					delete attr.rel;
					delete attr.target;
					component.setAttributes(attr);
				}
			}

		}
	});

	//Checkbox Button
	tm.addType('toggle_checkbox', {
		createInput({ trait }) {
			const el = document.createElement('div');
			el.classList.add("toggle-wrapper");
			el.id = trait.attributes.name;

			$(trait.attributes.options).each(function (index, value) {

				var input = document.createElement('input');
				var label = document.createElement("label");
				var icon = document.createElement("em");
				var img = document.createElement("img");

				input.setAttribute("type", "checkbox");
				input.setAttribute("name", trait.attributes.name);
				input.setAttribute("id", value.id);
				input.setAttribute("value", value.name);

				label.setAttribute("for", value.id);

				if (typeof value.icon != 'undefined') {
					icon.setAttribute("class", value.icon);
					label.appendChild(icon);
				}
				else if (typeof value.image != 'undefined') {
					img.setAttribute("src", VjDefaultPath + value.image + ".png");
					label.appendChild(img);
				}
				else {
					if (trait.target.attributes.type == "list")
						label.innerHTML = value.name
					else
						label.innerHTML = value.name.charAt(0).toUpperCase() + value.name.slice(1);
				}

				el.appendChild(input);
				el.appendChild(label);
			});

			var input = el.getElementsByTagName('input');
			var traitValue = trait.getInitValue();

			$(input).each(function () {
				if ($(this).attr("id") == traitValue)
					$(this).prop('checked', true);
			});

			return el;
		},
		eventCapture: ['input'],
		onEvent({ elInput, component, event }) {

			$(event.target.parentElement).find("input:checked").not(event.target).prop('checked', false);

			if (component.getTrait(event.target.name).attributes.UpdateStyles)
				UpdateStyles(elInput, component, event);
			else if (component.getTrait(event.target.name).attributes.SwitchClass)
				SwitchClass(elInput, component, event);
		}
	});

	//Radio Button
	tm.addType('toggle_radio', {
		createInput({ trait }) {
			const el = document.createElement('div');
			el.classList.add("toggle-wrapper");
			el.id = trait.attributes.name;

			$(trait.attributes.options).each(function (index, value) {

				var input = document.createElement('input');
				var label = document.createElement("label");
				var icon = document.createElement("em");
				var img = document.createElement("img");

				input.setAttribute("type", "radio");
				input.setAttribute("name", trait.attributes.name);
				input.setAttribute("id", value.id);
				input.setAttribute("value", value.name);

				if (trait.target.attributes.type == 'list' && trait.attributes.name == "ol_list_style")
					input.setAttribute("value", value.id);

				if (trait.target.attributes.type == 'grid' && trait.attributes.name == "reversecolumns")
					input.setAttribute("value", value.id);

				label.setAttribute("for", value.id);

				if (typeof value.icon != 'undefined') {
					icon.setAttribute("class", value.icon);
					label.appendChild(icon);
				}
				else if (typeof value.image != 'undefined') {
					img.setAttribute("src", VjDefaultPath + value.image + ".png");
					label.appendChild(img);
				}
				else {
					if (trait.target.attributes.type == 'list')
						label.innerHTML = value.name
					else
						label.innerHTML = value.name.charAt(0).toUpperCase() + value.name.slice(1);
				}

				el.appendChild(input);
				el.appendChild(label);
			});

			var input = el.getElementsByTagName('input');
			var traitValue = trait.getInitValue();

			$(input).each(function () {
				if ($(this).attr("id") == traitValue)
					$(this).prop('checked', true);
			});

			return el;
		},
		onUpdate({ elInput, component }) {

			if (component.attributes.type == 'list') {
				if (elInput.firstElementChild.name == "ul_list_style" && component.getTrait('list_type').getInitValue() == "ol")
					$(elInput).parents(".gjs-trt-trait__wrp").hide();
				else if (elInput.firstElementChild.name == "ol_list_style" && component.getTrait('list_type').getInitValue() == "ul")
					$(elInput).parents(".gjs-trt-trait__wrp").hide();
				else if (elInput.firstElementChild.name == "start" && component.getTrait('list_type').getInitValue() == "ul")
					$(elInput).parents(".gjs-trt-trait__wrp").hide();
			}

			if (component.attributes.type == 'icon') {
				if (elInput.firstElementChild.name == "framestyle" && component.getTrait('frame').getInitValue() == "none")
					$(elInput).parents(".gjs-trt-trait__wrp").hide();
			}

			if (component.attributes.type == 'section') {
				if (elInput.firstElementChild.name == "gradienttype" && component.getTrait('background').getInitValue() != "gradient")
					$(elInput).parents(".gjs-trt-trait__wrp").hide();

				if (elInput.firstElementChild.name == "angle" && component.getTrait('gradienttype').getInitValue() == "radial")
					$(elInput).parents(".gjs-trt-trait__wrp").hide();
			}
		},
		eventCapture: ['input'],
		onEvent({ elInput, component, event }) {

			if (component.attributes.type == 'list' && event.target.name == "list_type") {

				component.removeStyle("list-style-type");

				if (event.target.value == "ol") {

					var style = component.getStyle();
					style['padding-left'] = '40px';
					component.setStyle(style);

					$(component.getTrait("ul_list_style").el).parents(".gjs-trt-trait__wrp").hide();
					$(component.getTrait("start").el).parents(".gjs-trt-trait__wrp").show();
					$(component.getTrait("ol_list_style").el).parents(".gjs-trt-trait__wrp").show();
					$(component.getTrait("ol_list_style").el).find("input:checked").prop('checked', false);
					$(component.getTrait('ol_list_style').el).find('input#decimal').prop('checked', true);
				}
				else if (event.target.value == "ul") {
					$(component.getTrait("ol_list_style").el).parents(".gjs-trt-trait__wrp").hide();
					$(component.getTrait("start").el).parents(".gjs-trt-trait__wrp").hide();
					$(component.getTrait("ul_list_style").el).parents(".gjs-trt-trait__wrp").show();
					$(component.getTrait("ul_list_style").el).find("input:checked").prop('checked', false);
					$(component.getTrait('ul_list_style').el).find('input#disc').prop('checked', true);
				}
			}

			if (component.attributes.type == 'icon') {

				if (event.target.name == "frame") {
					if (event.target.value != "none") {
						$(component.getTrait("framewidth").el).parents(".gjs-trt-trait__wrp").show();
						$(component.getTrait("framegap").el).parents(".gjs-trt-trait__wrp").show();
						$(component.getTrait("framestyle").el).parents(".gjs-trt-trait__wrp").show();
						$(component.getTrait("framecolor").el).parents(".gjs-trt-trait__wrp").show();
						$(component.getTrait("framecolor").el).find("input#frame" + component.getTrait("framecolor").attributes.value).prop('checked', true).next().addClass('active');
					}
					else {
						$(component.getTrait("framewidth").el).parents(".gjs-trt-trait__wrp").hide();
						$(component.getTrait("framegap").el).parents(".gjs-trt-trait__wrp").hide();
						$(component.getTrait("framestyle").el).parents(".gjs-trt-trait__wrp").hide();
						$(component.getTrait("framecolor").el).parents(".gjs-trt-trait__wrp").hide();
					}
				}

				if (event.target.name == "background") {
					if (event.target.value == "fill") {
						$(component.getTrait("backgroundcolor").el).parents(".gjs-trt-trait__wrp").show();
						$(component.getTrait("backgroundcolor").el).find("input#color" + component.getTrait("backgroundcolor").attributes.value).prop('checked', true).next().addClass('active');
					}
					else {
						$(component.getTrait("backgroundcolor").el).parents(".gjs-trt-trait__wrp").hide();
						$(component.getTrait("backgroundcolor").el).find("input:checked").prop('checked', false);
						$(component.getTrait("backgroundcolor").el).find(".active").removeClass("active");
					}
				}
			}

			if (component.attributes.type == 'section') {
				if (event.target.name == "background") {

					component.addStyle({ 'overflow': 'visible' });
					component.set({ 'src': '', 'thumbnail': '' });

					if (event.target.value == "image") {

						$(component.getTrait("backgroundcolor").el).find("input:checked").prop('checked', false);
						$(component.getTrait("backgroundcolor").el).find(".active").removeClass("active");
						$(component.getTrait("backgroundimage").el).removeAttr("style");
						$(component.getTrait("imageposition").el).find("select").val("center center");
						$(component.getTrait("imageattachment").el).find("select").val("scroll");
						$(component.getTrait("imagerepeat").el).find("select").val("no-repeat");
						$(component.getTrait("imagesize").el).find("select").val("auto");

						$(component.getTrait("backgroundcolor").el).parents(".gjs-trt-trait__wrp").show();
						$(component.getTrait("backgroundimage").el).parents(".gjs-trt-trait__wrp").show();

						$(component.getTrait("imageposition").el).parents(".gjs-trt-trait__wrp").hide();
						$(component.getTrait("imageattachment").el).parents(".gjs-trt-trait__wrp").hide();
						$(component.getTrait("imagerepeat").el).parents(".gjs-trt-trait__wrp").hide();
						$(component.getTrait("imagesize").el).parents(".gjs-trt-trait__wrp").hide();

						$(component.getTrait("gradient").el).parents(".gjs-trt-trait__wrp").hide();
						$(component.getTrait("gradienttype").el).parents(".gjs-trt-trait__wrp").hide();
						$(component.getTrait("angle").el).parents(".gjs-trt-trait__wrp").hide();

						$(component.getTrait("backgroundvideo").el).parents(".gjs-trt-trait__wrp").hide();
						component.components().forEach(item => item.getAttributes()["data-bg-video"] == 'true' ? item.remove() : null);

						var style = component.getStyle();
						style["background-position"] = "center center";
						style["background-attachment"] = "scroll";
						style["background-repeat"] = "no-repeat";
						style["background-size"] = "auto";
						component.setStyle(style);

					}
					else if (event.target.value == "gradient") {

						$(component.getTrait("gradient").el).parents(".gjs-trt-trait__wrp").show();
						$(component.getTrait("gradienttype").el).parents(".gjs-trt-trait__wrp").show();
						$(component.getTrait("angle").el).parents(".gjs-trt-trait__wrp").show();

						$(component.getTrait("backgroundcolor").el).parents(".gjs-trt-trait__wrp").hide();
						$(component.getTrait("backgroundimage").el).parents(".gjs-trt-trait__wrp").hide();
						$(component.getTrait("imageposition").el).parents(".gjs-trt-trait__wrp").hide();
						$(component.getTrait("imageattachment").el).parents(".gjs-trt-trait__wrp").hide();
						$(component.getTrait("imagerepeat").el).parents(".gjs-trt-trait__wrp").hide();
						$(component.getTrait("imagesize").el).parents(".gjs-trt-trait__wrp").hide();

						$(component.getTrait("backgroundvideo").el).parents(".gjs-trt-trait__wrp").hide();
						component.components().forEach(item => item.getAttributes()["data-bg-video"] == 'true' ? item.remove() : null);

						$(component.getTrait("gradienttype").el).find('input#linear').prop('checked', true);
						$(component.getTrait("angle").el).find('input').val('90');

						if (typeof component.getTrait('gradient').view.gp != 'undefined') {
							const gp = component.getTrait('gradient').view.gp;
							gp.setValue("linear-gradient(90deg, #a1c4fd 10%, #c2e9fb 90%)");

							var style = component.getStyle();
							style["background-image"] = gp.getSafeValue();
							component.setStyle(style);
						}

					}
					else if (event.target.value == "video") {

						$(component.getTrait("backgroundcolor").el).parents(".gjs-trt-trait__wrp").hide();
						$(component.getTrait("backgroundimage").el).parents(".gjs-trt-trait__wrp").hide();
						$(component.getTrait("imageposition").el).parents(".gjs-trt-trait__wrp").hide();
						$(component.getTrait("imageattachment").el).parents(".gjs-trt-trait__wrp").hide();
						$(component.getTrait("imagerepeat").el).parents(".gjs-trt-trait__wrp").hide();
						$(component.getTrait("imagesize").el).parents(".gjs-trt-trait__wrp").hide();

						$(component.getTrait("gradient").el).parents(".gjs-trt-trait__wrp").hide();
						$(component.getTrait("gradienttype").el).parents(".gjs-trt-trait__wrp").hide();
						$(component.getTrait("angle").el).parents(".gjs-trt-trait__wrp").hide();

						$(component.getTrait("backgroundvideo").el).removeAttr("style");
						$(component.getTrait("backgroundvideo").el).parents(".gjs-trt-trait__wrp").show();

						var target = VjEditor.getSelected()
						window.document.vj_video_target = target;
						var url = CurrentExtTabUrl + "&guid=a7a5e633-a33a-4792-8149-bc15b9433505" + "&issupportbackground=false";
						OpenPopUp(null, 900, 'right', VjLocalized.Video, url, '', true);

					}
					else {

						$(component.getTrait("backgroundcolor").el).parents(".gjs-trt-trait__wrp").hide();
						$(component.getTrait("backgroundimage").el).parents(".gjs-trt-trait__wrp").hide();
						$(component.getTrait("imageposition").el).parents(".gjs-trt-trait__wrp").hide();
						$(component.getTrait("imageattachment").el).parents(".gjs-trt-trait__wrp").hide();
						$(component.getTrait("imagerepeat").el).parents(".gjs-trt-trait__wrp").hide();
						$(component.getTrait("imagesize").el).parents(".gjs-trt-trait__wrp").hide();

						$(component.getTrait("gradient").el).parents(".gjs-trt-trait__wrp").hide();
						$(component.getTrait("gradienttype").el).parents(".gjs-trt-trait__wrp").hide();
						$(component.getTrait("angle").el).parents(".gjs-trt-trait__wrp").hide();

						$(component.getTrait("backgroundvideo").el).parents(".gjs-trt-trait__wrp").hide();
						component.components().forEach(item => item.getAttributes()["data-bg-video"] == 'true' ? item.remove() : null);

					}
				}
				else if (event.target.name == "gradienttype") {

					component.getTrait("gradienttype").setTargetValue(event.target.value);

					if (typeof component.getTrait('gradient').view.gp != 'undefined') {

						const gp = component.getTrait('gradient').view.gp;
						gp.setType(event.target.value);

						if (event.target.value == 'radial') {
							$(component.getTrait("angle").el).parents(".gjs-trt-trait__wrp").hide();
							gp.setDirection('center center');
						}
						else if (event.target.value == 'linear') {
							$(component.getTrait("angle").el).parents(".gjs-trt-trait__wrp").show();
							$(component.getTrait("angle").el).find('input').val('90');
							gp.setDirection('90deg');
						}

						var style = component.getStyle();
						style["background-image"] = gp.getSafeValue();
						component.setStyle(style);
					}
				}
			}

			if (component.attributes.type == "video") {

				var trait = component.getTrait(event.target.name);
				trait.set({
					'value': event.target.id
				});

				if (component.getTrait("provider").getInitValue() == 'so') {
					if (event.target.name == "autoplay") {
						if (event.target.value == "yes") {
							component.set({ 'autoplay': 'autoplaytrue' });
							component.components().models[0].set({ 'autoplay': 1 });
							component.components().models[0].addAttributes({ 'autoplay': true, 'muted': true });
						}
						else {
							component.set({ 'autoplay': 'autoplayfalse' });
							component.components().models[0].set('autoplay', 0);
							var attr = component.components().models[0].attributes;
							delete attr.autoplay;
							component.components().models[0].setAttributes(attr);
						}
					}
					else if (event.target.name == "loop") {
						if (event.target.value == "yes") {
							component.set({ 'loop': 'looptrue' });
							component.components().models[0].set({ 'loop': 1 });
							component.components().models[0].addAttributes({ 'loop': true });
						}
						else {
							component.set({ 'loop': 'loopfalse' });
							component.components().models[0].set('loop', 0);
							var attr = component.components().models[0].attributes;
							delete attr.loop;
							component.components().models[0].setAttributes(attr);
						}
					}
					else if (event.target.name == "controls") {
						if (event.target.value == "yes") {
							component.set({ 'controls': 'controlstrue' });
							component.components().models[0].set({ 'controls': 1 });
							component.components().models[0].addAttributes({ 'controls': true });
						}
						else {
							component.set({ 'controls': 'controlsfalse' });
							component.components().models[0].set('controls', 0);
							component.components().models[0].addAttributes({ 'controls': false });
							var attr = component.components().models[0].attributes;
							delete attr.controls;
							component.components().models[0].setAttributes(attr);
						}
					}
				}
				else if (component.getTrait("provider").getInitValue() == 'yt') {

					var src = component.attributes.src;

					if (event.target.name == "autoplay") {
						if (event.target.value == "yes") {
							if (src.indexOf('mute') < 0) {
								if (src.indexOf('?') > 0)
									src += "&mute=1&autoplay=1";
								else
									src += "?mute=1&autoplay=1";
							}
							component.set({ 'autoplay': 'autoplaytrue' });
							component.components().models[0].set({ 'autoplay': 1 });
						}
						else {
							if (src.indexOf('mute') > 0) {
								if (src.indexOf('?mute') > 0 && src.match(/&/g) != null && src.match(/&/g).length == 1)
									src = src.replace('?mute=1&autoplay=1', '');
								else if (src.indexOf('&mute') > 0)
									src = src.replace('&mute=1&autoplay=1', '');
								else
									src = src.replace('mute=1&autoplay=1', '');
							}
							component.set({ 'autoplay': 'autoplayfalse' });
							component.components().models[0].set({ 'autoplay': 0 });
						}
					}
					else if (event.target.name == 'loop') {
						var vId = component.attributes.videoId;

						if (event.target.value == 'yes') {
							if (src.indexOf('loop') < 0) {
								if (src.indexOf('?') > 0)
									src += '&loop=1&playlist=' + vId;
								else
									src += '?loop=1&playlist=' + vId;
							}
							component.set({ 'loop': 'looptrue' });
							component.components().models[0].set({ 'loop': 0 });
						}
						else {
							if (src.indexOf('loop') > 0) {
								if (src.indexOf('?') > 0 && src.match(/&/g) != null && src.match(/&/g).length == 1)
									src = src.replace('?loop=1&playlist=' + vId, '');
								else if (src.indexOf('&loop') > 0)
									src = src.replace('&loop=1&playlist=' + vId, '');
								else
									src = src.replace('loop=1&playlist=' + vId, '');
							}
							component.set({ 'loop': 'loopfalse' });
							component.components().models[0].set({ 'loop': 0 });
						}
					}

					if (event.target.name == "rel") {
						if (event.target.value == "yes") {
							if (src.indexOf('rel') > 0) {
								if (src.indexOf('?rel') > 0 && src.match(/&/g) != null && src.match(/&/g).length == 0)
									src = src.replace('?rel=0', '');
								else if (src.indexOf('&rel') > 0)
									src = src.replace('&rel=0', '');
								else
									src = src.replace('rel=0', '');
							}
							component.set({ 'rel': 'reltrue' });
							component.components().models[0].set({ 'rel': 1 });
						}
						else {
							if (src.indexOf('rel') < 0) {
								if (src.indexOf('?') > 0)
									src += '&rel=0';
								else
									src += '?rel=0';
							}
							component.set({ 'rel': 'relfalse' });
							component.components().models[0].set({ 'rel': 0 });
						}
					}

					if (event.target.name == "logo") {
						if (event.target.value == "yes") {
							if (src.indexOf('modestbranding') > 0) {
								if (src.indexOf('?modestbranding') > 0 && src.match(/&/g) != null && src.match(/&/g).length == 0)
									src = src.replace('?modestbranding=1', '');
								else if (src.indexOf('&modestbranding') > 0)
									src = src.replace('&modestbranding=1', '');
								else
									src = src.replace('modestbranding=1', '');
							}
							component.set({ 'logo': 'logotrue' });
							component.components().models[0].set({ 'logo': 1 });
						}
						else {
							if (src.indexOf('modestbranding') < 0) {
								if (src.indexOf('?') > 0)
									src += '&modestbranding=1';
								else
									src += '?modestbranding=1';
							}
							component.set({ 'logo': 'logofalse' });
							component.components().models[0].set({ 'logo': 0 });
						}
					}

					if (src.indexOf('?') + 1 == src.indexOf('&'))
						src = src.replace('?&', '?');
					else if (src.indexOf('?') + 1 == src.length)
						src = src.replace('?', '');

					component.set({ 'src': src });
					component.components().models[0].set({ 'src': src, 'controls': 1 });
					component.components().models[0].addAttributes({ 'allow': 'autoplay' });;
					$(component.components().models[0].getEl()).find('iframe').attr('src', src);
				}

			}

			if (component.getTrait(event.target.name).attributes.UpdateStyles)
				UpdateStyles(elInput, component, event);
			else if (component.getTrait(event.target.name).attributes.SwitchClass)
				SwitchClass(elInput, component, event);

			if (component.attributes.type == "column" && component.components().length == 0)
				$(component.getEl()).attr("data-empty", "true");
		}
	});

	//Color
	tm.addType('custom_color', {
		createInput({ trait }) {

			const el = document.createElement('div');
			el.classList.add("color-wrapper");
			el.id = trait.attributes.name;

			$(trait.attributes.options).each(function (index, value) {

				var input = document.createElement('input');
				var label = document.createElement("label");
				var icon = document.createElement("em");

				input.setAttribute("type", "radio");
				input.setAttribute("name", trait.attributes.name);
				input.setAttribute("id", value.id);
				input.setAttribute("value", value.name);

				label.setAttribute("class", "bg-" + value.id);
				label.setAttribute("for", value.id);

				el.appendChild(input);
				el.appendChild(label);
			});

			var colorPicker = document.createElement('div');
			colorPicker.setAttribute("class", "colorPicker");
			colorPicker.setAttribute("id", trait.attributes.name);

			var that = this;
			var thisColorPicker = function () {
				return editor.TraitManager.getType('color').prototype.getInputEl.apply(that, arguments);
			}

			colorPicker.appendChild(thisColorPicker());
			el.appendChild(colorPicker);

			var colorField = el.querySelectorAll('.colorPicker .gjs-field-color-picker');

			$(colorField).click(function () {
				$(this).parents(".color-wrapper").find("input:checked").prop('checked', false);
				$(this).parents(".color-wrapper").find(".active").removeClass("active");

				var BGcolor = $(this).css("background-color");
				$(this).parents(".color-wrapper").find(".colorPicker").css("background-color", BGcolor).addClass('active');
			});

			$(editor.TraitManager.getType('color').prototype.getInputEl.apply(that, arguments)).on('move.spectrum change.spectrum', function (e, tinycolor) {
				CustomColor();

				var BGcolor = $(this).find(".gjs-field-color-picker").css("background-color");
				$(this).parents(".color-wrapper").find(".colorPicker").css("background-color", BGcolor);

				$(trait.target.getTrait('styles').el).find('label').css("color", BGcolor);

				$(trait.target.getTrait('styles').el).find('label').removeClass(function (index, className) {
					return (className.match(/\btext-\S+/g) || []).join(' ');
				});

				if (trait.target.attributes.type == 'heading' || trait.target.attributes.type == 'text') {

					var classes = jQuery.grep(trait.target.getClasses(), function (className, index) {
						return (className.match(/\btext-\S+/g) || []).join(' ');
					});

					trait.target.removeClass(classes);
				}

				if (trait.target.attributes.type == 'button') {

					var className = $(trait.target.getTrait('styles').el).find('label').attr('class').match(/\bbtn-\S+/g);

					$(trait.target.getTrait('styles').el).find('label').removeClass(className);

					if (trait.target.getTrait("stylee").getInitValue() == "outline") {
						$(trait.target.getTrait('styles').el).find('label').css({
							'color': BGcolor,
							'border-color': BGcolor,
						});
					}
					else if (trait.target.getTrait("stylee").getInitValue() == "fill") {
						$(trait.target.getTrait('styles').el).find('label').css({
							'background-color': BGcolor,
							'border-color': BGcolor,
							'color': "",
						});
					}
				}

			});

			var attr = trait.attributes.cssproperties[0].name;

			if (typeof VjEditor.getSelected().getStyle()[attr] != "undefined") {

				var customColor = VjEditor.getSelected().getStyle()[attr].replace("!important", "");

				if (customColor != "transparent") {
					el.querySelector('.gjs-field-color-picker').style.backgroundColor = customColor;
					el.querySelector('.colorPicker').style.backgroundColor = customColor;
					editor.$(el.querySelector('.gjs-field-color-picker')).spectrum("set", customColor);
				}
			}

			var input = el.getElementsByTagName('input');

			$(input).each(function () {
				if ($(this).attr("id") == trait.getInitValue()) {
					$(this).prop('checked', true);
					$(this).next().addClass("active");
				}
				else {
					$(this).prop('checked', false);
					$(this).removeClass("active");
				}
			});

			var getCustomColor = $(el).find('.colorPicker .gjs-field-color-picker').css('background-color');
			if (!$(el).find('input:checked').length)
				$(el).find('.colorPicker').addClass('active').css('background-color', getCustomColor);

			return el;
		},
		onUpdate({ elInput, component }) {
			if (component.attributes.type == 'icon') {

				if (elInput.parentElement.parentElement.firstElementChild.name == "framecolor" && component.getTrait('frame').getInitValue() == "none")
					$(elInput).parents(".gjs-trt-trait__wrp").hide();

				if (elInput.parentElement.parentElement.firstElementChild.name == "backgroundcolor" && component.getTrait('background').getInitValue() == "empty")
					$(elInput).parents(".gjs-trt-trait__wrp").hide();
			}

			if (component.attributes.type == 'section') {
				if (elInput.parentElement.parentElement.firstElementChild.name == "backgroundcolor" && (component.getTrait('background').getInitValue() == "none" || component.getTrait('background').getInitValue() == "gradient" || component.getTrait('background').getInitValue() == "video"))
					$(elInput).parents(".gjs-trt-trait__wrp").hide();
			}
		},
		eventCapture: ['input'],
		onEvent({ elInput, component, event }) {

			if (component.attributes.type == 'heading' || component.attributes.type == 'text') {
					var className = event.target.nextElementSibling.className;
					className = className.replace('bg', 'text');

					$(component.getTrait('styles').el).find('label').removeClass(function (index, css) {
						return (css.match(/\btext-\S+/g) || []).join(' ');
					});
					$(component.getTrait('styles').el).find('label').addClass(className);
				}
			
			$(event.target).parents(".color-wrapper").find(".colorPicker").css("background-color", "transparent");
			$(event.target).parents(".color-wrapper").find(".active").removeClass("active");
			$(event.target.nextElementSibling).addClass("active");

			var style = component.getStyle();
			style["background-color"] = "transparent";
			component.setStyle(style);

			SwitchClass(elInput, component, event);


		}
	});

	tm.addType('custom_range', {
		createInput({ trait }) {

			var traitValue = trait.getInitValue();

			const el = document.createElement('div');
			el.classList.add("range-wrapper");
			el.id = trait.attributes.name;

			el.innerHTML = `
				<input type="range" value="`+ traitValue + `" name="` + trait.attributes.name + `" min="` + trait.attributes.min + `" max="` + trait.attributes.max + `" class="range" /> 
				<input type="number" value="`+ traitValue + `" name="` + trait.attributes.name + `" min="` + trait.attributes.min + `" max="` + trait.attributes.max + `" class="number" />
			`;

			if (trait.attributes.unitOptions) {

				var div = document.createElement('div');
				div.setAttribute("class", "unit-wrapper");

				$(trait.attributes.units).each(function (index, value) {
					var input = document.createElement('input');
					var label = document.createElement("label");

					input.setAttribute('class', 'choose-unit');
					input.setAttribute('type', 'radio');
					input.setAttribute('name', trait.attributes.name);
					input.setAttribute('id', 'width' + value.name);
					input.setAttribute('value', value.name);

					label.setAttribute('for', 'width' + value.name);
					label.innerHTML = value.name;

					div.appendChild(input);
					div.appendChild(label);

				});

				el.appendChild(div);

				var unitVal = trait.target.attributes.unit;

				if (typeof unitVal == "undefined") {
					if (trait.target.attributes.type == 'heading' || trait.target.attributes.type == 'text' || trait.target.attributes.type == 'button' || trait.target.attributes.type == 'list')
						unitVal = "px";
					else if (trait.target.attributes.type == "divider")
						unitVal = "%";
				}


				var unitInput = el.querySelectorAll('.range-wrapper input.choose-unit');
				var input = el.querySelectorAll('.range-wrapper>input');

				$(unitInput).each(function () {
					if (unitVal == "%") {
						if (trait.target.attributes.type == "divider")
							$(this).parents('.range-wrapper').find('input').attr("max", "100");
						else if (trait.target.attributes.type == 'heading' || trait.target.attributes.type == 'text')
							$(this).parents('.range-wrapper').find('input').attr("min", "1").attr("max", "100").attr("step", "1");
					}
					else if (unitVal == "em") {
						if (trait.target.attributes.type == 'heading' || trait.target.attributes.type == 'text')
							$(this).parents('.range-wrapper').find('input').attr("min", ".1").attr("max", "10").attr("step", ".1");
					}
					else {
						if (trait.target.attributes.type == "divider")
							$(this).parents('.range-wrapper').find('input').attr("max", "1920");
						else if (trait.target.attributes.type == 'heading' || trait.target.attributes.type == 'text')
							$(this).parents('.range-wrapper').find('input').attr("min", "10").attr("max", "100").attr("step", "1");
					}
				});

				$(unitInput).each(function () {
					if ($(this).attr("value") == unitVal)
						$(this).prop('checked', true);
				});

				$(input).each(function () {
					$(this).attr("value", trait.getInitValue());
				});
			}

			return el;
		},
		onUpdate({ elInput, component }) {
			if (component.attributes.type == 'icon') {
				if ((elInput.firstElementChild.name == "framewidth" || elInput.firstElementChild.name == "framegap") && component.getTrait('frame').getInitValue() == "none")
					$(elInput).parents(".gjs-trt-trait__wrp").hide();
			}

			if (component.attributes.type == 'section') {
				if (elInput.firstElementChild.name == "angle") {
					if (component.getTrait('background').getInitValue() != "gradient" || (component.getTrait('background').getInitValue() == "gradient" && component.getTrait('background').getInitValue() == "radial"))
						$(elInput).parents(".gjs-trt-trait__wrp").hide();
				}
			}
		},
		eventCapture: ['input'],
		onEvent({ elInput, component, event }) {
			UpdateStyles(elInput, component, event);

			if (component.attributes.type == 'section' && event.target.name == "angle") {

				var angle = event.target.value;

				if (angle == '180')
					angle = '0';

				if (typeof component.getTrait('gradient').view.gp != 'undefined') {

					const gp = component.getTrait('gradient').view.gp;
					gp.setDirection(angle + 'deg');

					var style = component.getStyle();
					style["background-image"] = gp.getSafeValue();
					component.setStyle(style);
				}
			}

			var size = component.getStyle()['font-size'];

			if (component.attributes.type == 'heading' || component.attributes.type == 'text' || component.attributes.type == 'button')
				$(component.getTrait('styles').el).find('label').css('font-size', size);
		}
	});

	tm.addType('custom_number', {
		createInput({ trait }) {

			var traitValue = trait.getInitValue();

			const el = document.createElement('div');
			el.classList.add("number");
			el.id = trait.attributes.name;
			el.innerHTML = `<input type="number" value="` + traitValue + `" name="` + trait.attributes.name + `" min="` + trait.attributes.min + `" max="` + trait.attributes.max + `" class="number" />`;
			return el;
		},
		onUpdate({ elInput, component }) {
			if (component.attributes.type == 'list' && elInput.firstElementChild.name == "start" && component.getTrait('list_type').getInitValue() == "ul") {
				$(elInput).parents(".gjs-trt-trait__wrp").hide();
			}
			else if (component.attributes.type == 'carousel') {
				if (component.getTrait('automatically').getInitValue() == 'automaticallyfalse')
					$(elInput).parents('.gjs-trt-trait__wrp').hide();
				else
					$(elInput).parents('.gjs-trt-trait__wrp').show();
			}
		},
		eventCapture: ['input'],
		onEvent({ elInput, component, event }) {
			UpdateStyles(elInput, component, event);
		}
	});

	tm.addType('uploader', {
		createInput({ trait }) {

			const el = document.createElement('div');
			el.classList.add("uploader-wrapper");
			el.id = trait.attributes.name;

			el.innerHTML = `
			<input type="checkbox" name="`+ trait.attributes.name + `" for="upload">
			<label id="upload">
				<em class="fas fa-plus-circle"></em>
			</label>
			<span class="btn-delete"><em class="fas fa-trash"></em></span>
			`
			var selected = VjEditor.getSelected()
			var thumbnail = selected.attributes.thumbnail;

			if ((selected.attributes.background == "image" && selected.getStyle()["background-image"] == "none") || (selected.attributes.background == "video" && $(selected.getEl()).find(".bg-video").length == 0))
				thumbnail = ""

			if (typeof thumbnail != "undefined")
				el.setAttribute('style', 'background-image: url("' + thumbnail + '");');

			const imgDelete = el.querySelector('.btn-delete');

			imgDelete.addEventListener('click', ev => {
				if ($(ev.target).parents(".uploader-wrapper").attr("id") == "backgroundimage") {

					var comp = VjEditor.getSelected();
					comp.removeStyle("background-image");
					comp.set({ 'thumbnail': '', 'src': '' });

					$(comp.getTrait("backgroundimage").el).removeAttr("style");
					$(comp.getTrait("imageposition").el).parents(".gjs-trt-trait__wrp").hide();
					$(comp.getTrait("imageattachment").el).parents(".gjs-trt-trait__wrp").hide();
					$(comp.getTrait("imagerepeat").el).parents(".gjs-trt-trait__wrp").hide();
					$(comp.getTrait("imagesize").el).parents(".gjs-trt-trait__wrp").hide();
				}
				else if ($(ev.target).parents(".uploader-wrapper").attr("id") == "backgroundvideo") {

					var comp = VjEditor.getSelected();
					comp.components().forEach(item => item.getAttributes()["data-bg-video"] == 'true' ? item.remove() : null);
					comp.set({ 'thumbnail': '', 'src': '' });
					$(comp.getTrait("backgroundvideo").el).removeAttr("style");
				}
			});

			return el;
		},
		onUpdate({ elInput, component }) {
			if (component.attributes.type == 'section') {
				if (elInput.firstElementChild.name == "backgroundimage" && (component.getTrait('background').getInitValue() == "none" || component.getTrait('background').getInitValue() == "gradient" || component.getTrait('background').getInitValue() == "video"))
					$(elInput).parents(".gjs-trt-trait__wrp").hide();
				else if (elInput.firstElementChild.name == "backgroundvideo" && (component.getTrait('background').getInitValue() == "none" || component.getTrait('background').getInitValue() == "image" || component.getTrait('background').getInitValue() == "gradient"))
					$(elInput).parents(".gjs-trt-trait__wrp").hide();
			}
		},
		eventCapture: ['input'],
		onEvent({ elInput, component, event }) {
			if (event.type == "input") {
				if (event.target.name == "backgroundimage") {
					var target = VjEditor.getSelected();
					window.document.vj_image_target = target;
					var url = CurrentExtTabUrl + "&guid=a7a5e632-a73a-4792-8049-bc15a9435505#/setting";
					OpenPopUp(null, 900, 'right', 'Image', url, '', true);
				}
				else if (event.target.name == "backgroundvideo") {
					var target = VjEditor.getSelected()
					window.document.vj_video_target = target;
					var url = CurrentExtTabUrl + "&guid=a7a5e633-a33a-4792-8149-bc15b9433505" + "&issupportbackground=false";
					OpenPopUp(null, 900, 'right', VjLocalized.Video, url, '', true);
				}
			}
		}
	});

	tm.addType('dropdown', {
		createInput({ trait }) {

			const el = document.createElement('div');
			el.classList.add("select-wrapper");
			el.id = trait.attributes.name;

			var select = document.createElement('select');
			select.setAttribute("name", trait.attributes.name);

			$(trait.attributes.options).each(function (index, value) {
				var option = document.createElement("option");
				option.setAttribute("value", value.id);
				option.setAttribute("name", value.id);
				option.text = value.name;
				select.append(option);
			})

			var arrow = document.createElement('em');
			arrow.setAttribute("class", "fas fa-caret-down");

			el.appendChild(select);
			el.appendChild(arrow);

			$(select).val(trait.getInitValue());

			return el;
		},
		onUpdate({ elInput, component }) {
			if (component.attributes.type == 'section') {
				if ((elInput.id == "imageposition" || elInput.id == "imageattachment" || elInput.id == "imagerepeat" || elInput.id == "imagesize") && (component.getTrait('background').getInitValue() == "none" || component.getTrait('background').getInitValue() == "gradient" || component.getTrait('background').getInitValue() == "video"))
					$(elInput).parents(".gjs-trt-trait__wrp").hide();

				if (component.attributes["thumbnail"] == "") {
					$(component.getTrait("imageposition").el).parents(".gjs-trt-trait__wrp").hide();
					$(component.getTrait("imageattachment").el).parents(".gjs-trt-trait__wrp").hide();
					$(component.getTrait("imagerepeat").el).parents(".gjs-trt-trait__wrp").hide();
					$(component.getTrait("imagesize").el).parents(".gjs-trt-trait__wrp").hide();
				}
			}
		},
		onEvent({ elInput, component, event }) {
			if (component.attributes.type == 'video')
				if (event.target.value == 'yt')
					component.set({ 'provider': 'yt', 'videoId': '' });
				else
					component.set({ 'provider': 'so', 'src': '' });
			else
				UpdateStyles(elInput, component, event);
		}

	});

	tm.addType('gradient', {
		createInput({ trait }) {

			const el = document.createElement('div');
			el.setAttribute("class", "gradient-wrapper");
			el.setAttribute("id", "grapick");

			const colorEl = '<input id="colorpicker"/>';

			const gp = new Grapick({ colorEl, min: 1, max: 99, el });

			this.gp = gp;

			gp.on('change', function (complete) {
				if (gp.getSafeValue() != "") {
					var style = trait.target.getStyle();
					style["background-image"] = gp.getSafeValue();
					trait.target.setStyle(style);
				}
			});

			gp.emit('change');

			gp.setColorPicker(handler => {
				const el = handler.getEl().querySelector('#colorpicker');

				editor.$(el).spectrum({
					color: handler.getColor(),
					showAlpha: true,
					preferredFormat: "hex",
					showInput: true,
					chooseText: 'Ok',
					cancelText: '⨯',
					change(color) {
						handler.setColor(color.toRgbString());
					},
					move(color) {
						handler.setColor(color.toRgbString(), 0);
					}
				});
			});

			//gp.addHandler(1, '#085078', 0);
			//gp.addHandler(99, '#85D8CE', 0);

			return el;
		},
		onUpdate({ elInput, component }) {
			if (component.attributes.type == 'section' && component.getTrait('background').getInitValue() != "gradient")
				$(elInput).parents(".gjs-trt-trait__wrp").hide();

			if (component.getTrait('background').getInitValue() == "gradient" && (component.getStyle()['background-image'] != "" || component.getStyle()['background-image'] != "none")) {
				const gp = this.gp;
				gp.setValue(component.getStyle()['background-image']);
			}
		}
	});

	tm.addType('preset_radio', {

		createInput({ trait }) {

			var targetName = trait.target.attributes.type;

			const el = document.createElement('div');
			el.setAttribute("class", targetName + ' preset-wrapper');
			el.id = trait.attributes.name;

			$(trait.attributes.options).each(function (index, value) {

				var div = document.createElement('div');
				var input = document.createElement('input');
				var label = document.createElement("label");
				var icon = document.createElement("em");

				div.setAttribute("class", trait.attributes.name);

				input.setAttribute("type", "radio");
				input.setAttribute("name", trait.attributes.name);
				input.setAttribute("id", value.id);
				input.setAttribute("class", value.class);
				input.setAttribute("value", value.name);

				label.setAttribute("for", value.id);
				label.setAttribute("class", value.class + ' text-primary');
				label.innerHTML = trait.target.getEl().textContent;

				div.appendChild(input);
				div.appendChild(label);
				el.appendChild(div);
			});

			var input = el.getElementsByTagName('input');

			$(input).each(function () {
				if ($(this).attr("value") == trait.getInitValue()) {
					$(this).prop('checked', true);
					$(this).parent().addClass('active');
				}
			});

			return el;
		},
		onUpdate({ elInput, component }) {

			var compClass = component.getClasses();

			compClass = jQuery.grep(compClass, function (className, index) {
				return (className.match(/\btext-\S+/g) || []).join(' ');
			});

			$(elInput).find('label').removeClass(function (index, className) {
				return (className.match(/\btext-\S+/g) || []).join(' ');
			});

			$(elInput).find('label').addClass(compClass[0]).css('font-size', component.getStyle()['font-size']);

			if (typeof component.getStyle()['color'] != 'undefined') {
				var color = component.getStyle()['color'].replace('!important', '');
				$(elInput).find('label').css('color', color);
			}

			var text = component.getEl().textContent;
			text = text.split(/\s+/).slice(0, 20).join(" ");
			$(elInput).find('label').text(text);


			if (component.attributes.type == 'button') {

				if (typeof component.getStyle()['background-color'] != "undefined") {
					var bgcolor = component.getStyle()['background-color'].replace('!important', '');
					$(elInput).find('label').css('background-color', bgcolor);
				}

				if (typeof component.getStyle()['border-color'] != "undefined") {
					var bgcolor = component.getStyle()['border-color'].replace('!important', '');
					$(elInput).find('label').css('border-color', bgcolor);
				}

				$(elInput).css(component.parent().getStyle());

				$(component.getClasses()).each(function (index, className) {
					$(elInput).find('label').addClass(className);
				})

				if (typeof component.getStyle()['width'] != "undefined") {
					if ($(component.getEl()).outerWidth() < $(".sidebar .panel").outerWidth())
						$(elInput).find('label').css('width', $(component.getEl()).css('width'));
					else
						$(elInput).find('label').css('width', '100%');
				}

			}

			var font = component.view.$el.css('font-family');
			$(elInput).find('label').css('font-family', font);
		},
		onEvent({ elInput, component }) {

			//VjEditor.getSelected().setStyle();

			$(event.target).parents('.preset-wrapper').find('.active').removeClass('active');
			$(event.target).parent().addClass('active');

			component.getTrait('styles').set({
				'value': event.target.value
			});

			var className = event.target.className;
			var classes = component.getClasses();

			if (component.attributes.type == 'heading') {
				classes = jQuery.grep(classes, function (className, index) {
					return (className.match(/\bhead-style-\S+/g) || []).join(' ');

				});
			}
			else if (component.attributes.type == 'text') {
				classes = jQuery.grep(classes, function (className, index) {
					return (className.match(/\bparagraph-style-\S+/g) || []).join(' ');
				});
			}
			else if (component.attributes.type == 'button') {
				classes = jQuery.grep(classes, function (className, index) {
					return (className.match(/\bbutton-style-\S+/g) || []).join(' ');
				});
			}

			component.removeClass(classes);

			if (className != 'none')
				component.addClass(className);
		}

	});

}