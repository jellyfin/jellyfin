$(function() {

	// Helper function for vertically aligning DOM elements
	// http://www.seodenver.com/simple-vertical-align-plugin-for-jquery/
	$.fn.vAlign = function() {
		return this.each(function(i){
		var ah = $(this).height();
		var ph = $(this).parent().height();
		var mh = (ph - ah) / 2;
		$(this).css('margin-top', mh);
		});
	};

	$.fn.stretchFormtasticInputWidthToParent = function() {
		return this.each(function(i){
		var p_width = $(this).closest("form").innerWidth();
		var p_padding = parseInt($(this).closest("form").css('padding-left') ,10) + parseInt($(this).closest("form").css('padding-right'), 10);
		var this_padding = parseInt($(this).css('padding-left'), 10) + parseInt($(this).css('padding-right'), 10);
		$(this).css('width', p_width - p_padding - this_padding);
		});
	};

	$('form.formtastic li.string input, form.formtastic textarea').stretchFormtasticInputWidthToParent();

	// Vertically center these paragraphs
	// Parent may need a min-height for this to work..
	$('ul.downplayed li div.content p').vAlign();

	// When a sandbox form is submitted..
	$("form.sandbox").submit(function(){

		var error_free = true;

		// Cycle through the forms required inputs
 		$(this).find("input.required").each(function() {

			// Remove any existing error styles from the input
			$(this).removeClass('error');

			// Tack the error style on if the input is empty..
			if ($(this).val() == '') {
				$(this).addClass('error');
				$(this).wiggle();
				error_free = false;
			}

		});

		return error_free;
	});

});

function clippyCopiedCallback(a) {
  $('#api_key_copied').fadeIn().delay(1000).fadeOut();

  // var b = $("#clippy_tooltip_" + a);
  // b.length != 0 && (b.attr("title", "copied!").trigger("tipsy.reload"), setTimeout(function() {
  //   b.attr("title", "copy to clipboard")
  // },
  // 500))
}

// Logging function that accounts for browsers that don't have window.console
function log() {
  if (window.console) console.log.apply(console,arguments);
}
// Handle browsers that do console incorrectly (IE9 and below, see http://stackoverflow.com/a/5539378/7913)
if (Function.prototype.bind && console && typeof console.log == "object") {
    [
      "log","info","warn","error","assert","dir","clear","profile","profileEnd"
    ].forEach(function (method) {
        console[method] = this.bind(console[method], console);
    }, Function.prototype.call);
}

var Docs = {

	shebang: function() {

		// If shebang has an operation nickname in it..
		// e.g. /docs/#!/words/get_search
		var fragments = $.param.fragment().split('/');
		fragments.shift(); // get rid of the bang

		switch (fragments.length) {
			case 1:
				// Expand all operations for the resource and scroll to it
//				log('shebang resource:' + fragments[0]);
				var dom_id = 'resource_' + fragments[0];

				Docs.expandEndpointListForResource(fragments[0]);
				$("#"+dom_id).slideto({highlight: false});
				break;
			case 2:
				// Refer to the endpoint DOM element, e.g. #words_get_search
//				log('shebang endpoint: ' + fragments.join('_'));

                // Expand Resource
                Docs.expandEndpointListForResource(fragments[0]);
                $("#"+dom_id).slideto({highlight: false});

                // Expand operation
				var li_dom_id = fragments.join('_');
				var li_content_dom_id = li_dom_id + "_content";

//                log("li_dom_id " + li_dom_id);
//                log("li_content_dom_id " + li_content_dom_id);

				Docs.expandOperation($('#'+li_content_dom_id));
				$('#'+li_dom_id).slideto({highlight: false});
				break;
		}

	},

	toggleEndpointListForResource: function(resource) {
		var elem = $('li#resource_' + Docs.escapeResourceName(resource) + ' ul.endpoints');
		if (elem.is(':visible')) {
			Docs.collapseEndpointListForResource(resource);
		} else {
			Docs.expandEndpointListForResource(resource);
		}
	},

	// Expand resource
	expandEndpointListForResource: function(resource) {
		var resource = Docs.escapeResourceName(resource);
		if (resource == '') {
			$('.resource ul.endpoints').slideDown();
			return;
		}
		
		$('li#resource_' + resource).addClass('active');

		var elem = $('li#resource_' + resource + ' ul.endpoints');
		elem.slideDown();
	},

	// Collapse resource and mark as explicitly closed
	collapseEndpointListForResource: function(resource) {
		var resource = Docs.escapeResourceName(resource);
		$('li#resource_' + resource).removeClass('active');

		var elem = $('li#resource_' + resource + ' ul.endpoints');
		elem.slideUp();
	},

	expandOperationsForResource: function(resource) {
		// Make sure the resource container is open..
		Docs.expandEndpointListForResource(resource);
		
		if (resource == '') {
			$('.resource ul.endpoints li.operation div.content').slideDown();
			return;
		}

		$('li#resource_' + Docs.escapeResourceName(resource) + ' li.operation div.content').each(function() {
			Docs.expandOperation($(this));
		});
	},

	collapseOperationsForResource: function(resource) {
		// Make sure the resource container is open..
		Docs.expandEndpointListForResource(resource);

		$('li#resource_' + Docs.escapeResourceName(resource) + ' li.operation div.content').each(function() {
			Docs.collapseOperation($(this));
		});
	},

	escapeResourceName: function(resource) {
		return resource.replace(/[!"#$%&'()*+,.\/:;<=>?@\[\\\]\^`{|}~]/g, "\\$&");
	},

	expandOperation: function(elem) {
		elem.slideDown();
	},

	collapseOperation: function(elem) {
		elem.slideUp();
	}

};
(function() {
  var template = Handlebars.template, templates = Handlebars.templates = Handlebars.templates || {};
templates['content_type'] = template(function (Handlebars,depth0,helpers,partials,data) {
  helpers = helpers || Handlebars.helpers;
  var buffer = "", stack1, stack2, foundHelper, tmp1, self=this, functionType="function", helperMissing=helpers.helperMissing, undef=void 0;

function program1(depth0,data) {
  
  var buffer = "", stack1, stack2;
  buffer += "\n    ";
  foundHelper = helpers.produces;
  stack1 = foundHelper || depth0.produces;
  stack2 = helpers.each;
  tmp1 = self.program(2, program2, data);
  tmp1.hash = {};
  tmp1.fn = tmp1;
  tmp1.inverse = self.noop;
  stack1 = stack2.call(depth0, stack1, tmp1);
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "\n";
  return buffer;}
function program2(depth0,data) {
  
  var buffer = "", stack1;
  buffer += "\n	<option value=\"";
  stack1 = depth0;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "this", { hash: {} }); }
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "\">";
  stack1 = depth0;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "this", { hash: {} }); }
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "</option>\n	";
  return buffer;}

function program4(depth0,data) {
  
  
  return "\n    <option value=\"application/json\">application/json</option>\n";}

  buffer += "<label for=\"contentType\"></label>\n<select name=\"contentType\">\n";
  foundHelper = helpers.produces;
  stack1 = foundHelper || depth0.produces;
  stack2 = helpers['if'];
  tmp1 = self.program(1, program1, data);
  tmp1.hash = {};
  tmp1.fn = tmp1;
  tmp1.inverse = self.program(4, program4, data);
  stack1 = stack2.call(depth0, stack1, tmp1);
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "\n</select>\n";
  return buffer;});
})();

(function() {
  var template = Handlebars.template, templates = Handlebars.templates = Handlebars.templates || {};
templates['main'] = template(function (Handlebars,depth0,helpers,partials,data) {
  helpers = helpers || Handlebars.helpers;
  var buffer = "", stack1, stack2, foundHelper, tmp1, self=this, functionType="function", helperMissing=helpers.helperMissing, undef=void 0, escapeExpression=this.escapeExpression;

function program1(depth0,data) {
  
  var buffer = "", stack1;
  buffer += "\n        , <span style=\"font-variant: small-caps\">api version</span>: ";
  foundHelper = helpers.apiVersion;
  stack1 = foundHelper || depth0.apiVersion;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "apiVersion", { hash: {} }); }
  buffer += escapeExpression(stack1) + "\n        ";
  return buffer;}

  buffer += "\n<div class='container' id='resources_container'>\n    <ul id='resources'>\n    </ul>\n\n    <div class=\"footer\">\n        <br>\n        <br>\n        <h4 style=\"color: #999\">[ <span style=\"font-variant: small-caps\">base url</span>: ";
  foundHelper = helpers.basePath;
  stack1 = foundHelper || depth0.basePath;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "basePath", { hash: {} }); }
  buffer += escapeExpression(stack1) + "\n        ";
  foundHelper = helpers.apiVersion;
  stack1 = foundHelper || depth0.apiVersion;
  stack2 = helpers['if'];
  tmp1 = self.program(1, program1, data);
  tmp1.hash = {};
  tmp1.fn = tmp1;
  tmp1.inverse = self.noop;
  stack1 = stack2.call(depth0, stack1, tmp1);
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "]</h4>\n    </div>\n</div>\n";
  return buffer;});
})();

(function() {
  var template = Handlebars.template, templates = Handlebars.templates = Handlebars.templates || {};
templates['operation'] = template(function (Handlebars,depth0,helpers,partials,data) {
  helpers = helpers || Handlebars.helpers;
  var buffer = "", stack1, stack2, foundHelper, tmp1, self=this, functionType="function", helperMissing=helpers.helperMissing, undef=void 0, escapeExpression=this.escapeExpression;

function program1(depth0,data) {
  
  var buffer = "", stack1;
  buffer += "\n                <h4>Implementation Notes</h4>\n                <p>";
  foundHelper = helpers.notes;
  stack1 = foundHelper || depth0.notes;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "notes", { hash: {} }); }
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "</p>\n                ";
  return buffer;}

function program3(depth0,data) {
  
  
  return "\n                    <h4>Response Class</h4>\n                    <p><span class=\"model-signature\" /></p>\n                    <br/>\n                    <div class=\"content-type\" />\n                ";}

function program5(depth0,data) {
  
  
  return "\n                    <h4>Parameters</h4>\n                    <table class='fullwidth'>\n                        <thead>\n                        <tr>\n                            <th style=\"width: 100px; max-width: 100px\">Parameter</th>\n                            <th style=\"width: 310px; max-width: 310px\">Value</th>\n                            <th style=\"width: 200px; max-width: 200px\">Description</th>\n                            <th style=\"width: 100px; max-width: 100px\">Parameter Type</th>\n                            <th style=\"width: 220px; max-width: 230px\">Data Type</th>\n                        </tr>\n                        </thead>\n                        <tbody class=\"operation-params\">\n\n                        </tbody>\n                    </table>\n                    ";}

function program7(depth0,data) {
  
  
  return "\n                    <div style='margin:0;padding:0;display:inline'></div>\n                    <h4>Error Status Codes</h4>\n                    <table class='fullwidth'>\n                        <thead>\n                        <tr>\n                            <th>HTTP Status Code</th>\n                            <th>Reason</th>\n                        </tr>\n                        </thead>\n                        <tbody class=\"operation-status\">\n                        \n                        </tbody>\n                    </table>\n                    ";}

function program9(depth0,data) {
  
  
  return "\n                    ";}

function program11(depth0,data) {
  
  
  return "\n                    <div class='sandbox_header'>\n                        <input class='submit' name='commit' type='button' value='Try it out!' />\n                        <a href='#' class='response_hider' style='display:none'>Hide Response</a>\n                        <img alt='Throbber' class='response_throbber' src='images/throbber.gif' style='display:none' />\n                    </div>\n                    ";}

  buffer += "\n    <ul class='operations' >\n      <li class='";
  foundHelper = helpers.httpMethod;
  stack1 = foundHelper || depth0.httpMethod;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "httpMethod", { hash: {} }); }
  buffer += escapeExpression(stack1) + " operation' id='";
  foundHelper = helpers.resourceName;
  stack1 = foundHelper || depth0.resourceName;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "resourceName", { hash: {} }); }
  buffer += escapeExpression(stack1) + "_";
  foundHelper = helpers.nickname;
  stack1 = foundHelper || depth0.nickname;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "nickname", { hash: {} }); }
  buffer += escapeExpression(stack1) + "_";
  foundHelper = helpers.httpMethod;
  stack1 = foundHelper || depth0.httpMethod;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "httpMethod", { hash: {} }); }
  buffer += escapeExpression(stack1) + "_";
  foundHelper = helpers.number;
  stack1 = foundHelper || depth0.number;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "number", { hash: {} }); }
  buffer += escapeExpression(stack1) + "'>\n            <div class='heading'>\n                <h3>\n                  <span class='http_method'>\n                    <a href='#!/";
  foundHelper = helpers.resourceName;
  stack1 = foundHelper || depth0.resourceName;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "resourceName", { hash: {} }); }
  buffer += escapeExpression(stack1) + "/";
  foundHelper = helpers.nickname;
  stack1 = foundHelper || depth0.nickname;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "nickname", { hash: {} }); }
  buffer += escapeExpression(stack1) + "_";
  foundHelper = helpers.httpMethod;
  stack1 = foundHelper || depth0.httpMethod;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "httpMethod", { hash: {} }); }
  buffer += escapeExpression(stack1) + "_";
  foundHelper = helpers.number;
  stack1 = foundHelper || depth0.number;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "number", { hash: {} }); }
  buffer += escapeExpression(stack1) + "' class=\"toggleOperation\">";
  foundHelper = helpers.httpMethod;
  stack1 = foundHelper || depth0.httpMethod;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "httpMethod", { hash: {} }); }
  buffer += escapeExpression(stack1) + "</a>\n                  </span>\n                  <span class='path'>\n                    <a href='#!/";
  foundHelper = helpers.resourceName;
  stack1 = foundHelper || depth0.resourceName;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "resourceName", { hash: {} }); }
  buffer += escapeExpression(stack1) + "/";
  foundHelper = helpers.nickname;
  stack1 = foundHelper || depth0.nickname;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "nickname", { hash: {} }); }
  buffer += escapeExpression(stack1) + "_";
  foundHelper = helpers.httpMethod;
  stack1 = foundHelper || depth0.httpMethod;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "httpMethod", { hash: {} }); }
  buffer += escapeExpression(stack1) + "_";
  foundHelper = helpers.number;
  stack1 = foundHelper || depth0.number;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "number", { hash: {} }); }
  buffer += escapeExpression(stack1) + "' class=\"toggleOperation\">";
  foundHelper = helpers.path;
  stack1 = foundHelper || depth0.path;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "path", { hash: {} }); }
  buffer += escapeExpression(stack1) + "</a>\n                  </span>\n                </h3>\n                <ul class='options'>\n                    <li>\n                    <a href='#!/";
  foundHelper = helpers.resourceName;
  stack1 = foundHelper || depth0.resourceName;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "resourceName", { hash: {} }); }
  buffer += escapeExpression(stack1) + "/";
  foundHelper = helpers.nickname;
  stack1 = foundHelper || depth0.nickname;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "nickname", { hash: {} }); }
  buffer += escapeExpression(stack1) + "_";
  foundHelper = helpers.httpMethod;
  stack1 = foundHelper || depth0.httpMethod;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "httpMethod", { hash: {} }); }
  buffer += escapeExpression(stack1) + "_";
  foundHelper = helpers.number;
  stack1 = foundHelper || depth0.number;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "number", { hash: {} }); }
  buffer += escapeExpression(stack1) + "' class=\"toggleOperation\">";
  foundHelper = helpers.summary;
  stack1 = foundHelper || depth0.summary;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "summary", { hash: {} }); }
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "</a>\n                    </li>\n                </ul>\n            </div>\n            <div class='content' id='";
  foundHelper = helpers.resourceName;
  stack1 = foundHelper || depth0.resourceName;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "resourceName", { hash: {} }); }
  buffer += escapeExpression(stack1) + "_";
  foundHelper = helpers.nickname;
  stack1 = foundHelper || depth0.nickname;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "nickname", { hash: {} }); }
  buffer += escapeExpression(stack1) + "_";
  foundHelper = helpers.httpMethod;
  stack1 = foundHelper || depth0.httpMethod;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "httpMethod", { hash: {} }); }
  buffer += escapeExpression(stack1) + "_";
  foundHelper = helpers.number;
  stack1 = foundHelper || depth0.number;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "number", { hash: {} }); }
  buffer += escapeExpression(stack1) + "_content' style='display:none'>\n                ";
  foundHelper = helpers.notes;
  stack1 = foundHelper || depth0.notes;
  stack2 = helpers['if'];
  tmp1 = self.program(1, program1, data);
  tmp1.hash = {};
  tmp1.fn = tmp1;
  tmp1.inverse = self.noop;
  stack1 = stack2.call(depth0, stack1, tmp1);
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "\n                ";
  foundHelper = helpers.responseClass;
  stack1 = foundHelper || depth0.responseClass;
  stack2 = helpers['if'];
  tmp1 = self.program(3, program3, data);
  tmp1.hash = {};
  tmp1.fn = tmp1;
  tmp1.inverse = self.noop;
  stack1 = stack2.call(depth0, stack1, tmp1);
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "\n                <form accept-charset='UTF-8' class='sandbox'>\n                    <div style='margin:0;padding:0;display:inline'></div>\n                    ";
  foundHelper = helpers.parameters;
  stack1 = foundHelper || depth0.parameters;
  stack2 = helpers['if'];
  tmp1 = self.program(5, program5, data);
  tmp1.hash = {};
  tmp1.fn = tmp1;
  tmp1.inverse = self.noop;
  stack1 = stack2.call(depth0, stack1, tmp1);
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "\n                    ";
  foundHelper = helpers.errorResponses;
  stack1 = foundHelper || depth0.errorResponses;
  stack2 = helpers['if'];
  tmp1 = self.program(7, program7, data);
  tmp1.hash = {};
  tmp1.fn = tmp1;
  tmp1.inverse = self.noop;
  stack1 = stack2.call(depth0, stack1, tmp1);
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "\n                    ";
  foundHelper = helpers.isReadOnly;
  stack1 = foundHelper || depth0.isReadOnly;
  stack2 = helpers['if'];
  tmp1 = self.program(9, program9, data);
  tmp1.hash = {};
  tmp1.fn = tmp1;
  tmp1.inverse = self.program(11, program11, data);
  stack1 = stack2.call(depth0, stack1, tmp1);
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "\n                </form>\n                <div class='response' style='display:none'>\n                    <h4>Request URL</h4>\n                    <div class='block request_url'></div>\n                    <h4>Response Body</h4>\n                    <div class='block response_body'></div>\n                    <h4>Response Code</h4>\n                    <div class='block response_code'></div>\n                    <h4>Response Headers</h4>\n                    <div class='block response_headers'></div>\n                </div>\n            </div>\n        </li>\n    </ul>\n";
  return buffer;});
})();

(function() {
  var template = Handlebars.template, templates = Handlebars.templates = Handlebars.templates || {};
templates['param'] = template(function (Handlebars,depth0,helpers,partials,data) {
  helpers = helpers || Handlebars.helpers;
  var buffer = "", stack1, stack2, foundHelper, tmp1, self=this, functionType="function", helperMissing=helpers.helperMissing, undef=void 0, escapeExpression=this.escapeExpression;

function program1(depth0,data) {
  
  var buffer = "", stack1, stack2;
  buffer += "\n		";
  foundHelper = helpers.isFile;
  stack1 = foundHelper || depth0.isFile;
  stack2 = helpers['if'];
  tmp1 = self.program(2, program2, data);
  tmp1.hash = {};
  tmp1.fn = tmp1;
  tmp1.inverse = self.program(4, program4, data);
  stack1 = stack2.call(depth0, stack1, tmp1);
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "\n	";
  return buffer;}
function program2(depth0,data) {
  
  var buffer = "", stack1;
  buffer += "\n			<input type=\"file\" name='";
  foundHelper = helpers.name;
  stack1 = foundHelper || depth0.name;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "name", { hash: {} }); }
  buffer += escapeExpression(stack1) + "'/>\n		";
  return buffer;}

function program4(depth0,data) {
  
  var buffer = "", stack1, stack2;
  buffer += "\n			";
  foundHelper = helpers.defaultValue;
  stack1 = foundHelper || depth0.defaultValue;
  stack2 = helpers['if'];
  tmp1 = self.program(5, program5, data);
  tmp1.hash = {};
  tmp1.fn = tmp1;
  tmp1.inverse = self.program(7, program7, data);
  stack1 = stack2.call(depth0, stack1, tmp1);
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "\n		";
  return buffer;}
function program5(depth0,data) {
  
  var buffer = "", stack1;
  buffer += "\n				<textarea class='body-textarea' name='";
  foundHelper = helpers.name;
  stack1 = foundHelper || depth0.name;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "name", { hash: {} }); }
  buffer += escapeExpression(stack1) + "'>";
  foundHelper = helpers.defaultValue;
  stack1 = foundHelper || depth0.defaultValue;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "defaultValue", { hash: {} }); }
  buffer += escapeExpression(stack1) + "</textarea>\n			";
  return buffer;}

function program7(depth0,data) {
  
  var buffer = "", stack1;
  buffer += "\n				<textarea class='body-textarea' name='";
  foundHelper = helpers.name;
  stack1 = foundHelper || depth0.name;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "name", { hash: {} }); }
  buffer += escapeExpression(stack1) + "'></textarea>\n				<br />\n				<div class=\"content-type\" />\n			";
  return buffer;}

function program9(depth0,data) {
  
  var buffer = "", stack1, stack2;
  buffer += "\n		";
  foundHelper = helpers.defaultValue;
  stack1 = foundHelper || depth0.defaultValue;
  stack2 = helpers['if'];
  tmp1 = self.program(10, program10, data);
  tmp1.hash = {};
  tmp1.fn = tmp1;
  tmp1.inverse = self.program(12, program12, data);
  stack1 = stack2.call(depth0, stack1, tmp1);
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "\n	";
  return buffer;}
function program10(depth0,data) {
  
  var buffer = "", stack1;
  buffer += "\n			<input class='parameter' minlength='0' name='";
  foundHelper = helpers.name;
  stack1 = foundHelper || depth0.name;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "name", { hash: {} }); }
  buffer += escapeExpression(stack1) + "' placeholder='' type='text' value='";
  foundHelper = helpers.defaultValue;
  stack1 = foundHelper || depth0.defaultValue;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "defaultValue", { hash: {} }); }
  buffer += escapeExpression(stack1) + "'/>\n		";
  return buffer;}

function program12(depth0,data) {
  
  var buffer = "", stack1;
  buffer += "\n			<input class='parameter' minlength='0' name='";
  foundHelper = helpers.name;
  stack1 = foundHelper || depth0.name;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "name", { hash: {} }); }
  buffer += escapeExpression(stack1) + "' placeholder='' type='text' value=''/>\n		";
  return buffer;}

  buffer += "<td class='code'>";
  foundHelper = helpers.name;
  stack1 = foundHelper || depth0.name;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "name", { hash: {} }); }
  buffer += escapeExpression(stack1) + "</td>\n<td>\n\n	";
  foundHelper = helpers.isBody;
  stack1 = foundHelper || depth0.isBody;
  stack2 = helpers['if'];
  tmp1 = self.program(1, program1, data);
  tmp1.hash = {};
  tmp1.fn = tmp1;
  tmp1.inverse = self.program(9, program9, data);
  stack1 = stack2.call(depth0, stack1, tmp1);
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "\n\n</td>\n<td>";
  foundHelper = helpers.description;
  stack1 = foundHelper || depth0.description;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "description", { hash: {} }); }
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "</td>\n<td>";
  foundHelper = helpers.paramType;
  stack1 = foundHelper || depth0.paramType;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "paramType", { hash: {} }); }
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "</td>\n<td>\n	<span class=\"model-signature\"></span>\n</td>\n\n";
  return buffer;});
})();

(function() {
  var template = Handlebars.template, templates = Handlebars.templates = Handlebars.templates || {};
templates['param_list'] = template(function (Handlebars,depth0,helpers,partials,data) {
  helpers = helpers || Handlebars.helpers;
  var buffer = "", stack1, stack2, foundHelper, tmp1, self=this, functionType="function", helperMissing=helpers.helperMissing, undef=void 0, escapeExpression=this.escapeExpression;

function program1(depth0,data) {
  
  
  return "\n        ";}

function program3(depth0,data) {
  
  var buffer = "", stack1, stack2;
  buffer += "\n            ";
  foundHelper = helpers.defaultValue;
  stack1 = foundHelper || depth0.defaultValue;
  stack2 = helpers['if'];
  tmp1 = self.program(4, program4, data);
  tmp1.hash = {};
  tmp1.fn = tmp1;
  tmp1.inverse = self.program(6, program6, data);
  stack1 = stack2.call(depth0, stack1, tmp1);
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "\n        ";
  return buffer;}
function program4(depth0,data) {
  
  
  return "\n            ";}

function program6(depth0,data) {
  
  
  return "\n                <option selected=\"\" value=''></option>\n            ";}

function program8(depth0,data) {
  
  var buffer = "", stack1, stack2;
  buffer += "\n            ";
  foundHelper = helpers.isDefault;
  stack1 = foundHelper || depth0.isDefault;
  stack2 = helpers['if'];
  tmp1 = self.program(9, program9, data);
  tmp1.hash = {};
  tmp1.fn = tmp1;
  tmp1.inverse = self.program(11, program11, data);
  stack1 = stack2.call(depth0, stack1, tmp1);
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "\n        ";
  return buffer;}
function program9(depth0,data) {
  
  var buffer = "", stack1;
  buffer += "\n                <option value='";
  foundHelper = helpers.value;
  stack1 = foundHelper || depth0.value;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "value", { hash: {} }); }
  buffer += escapeExpression(stack1) + "'>";
  foundHelper = helpers.value;
  stack1 = foundHelper || depth0.value;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "value", { hash: {} }); }
  buffer += escapeExpression(stack1) + " (default)</option>\n            ";
  return buffer;}

function program11(depth0,data) {
  
  var buffer = "", stack1;
  buffer += "\n                <option value='";
  foundHelper = helpers.value;
  stack1 = foundHelper || depth0.value;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "value", { hash: {} }); }
  buffer += escapeExpression(stack1) + "'>";
  foundHelper = helpers.value;
  stack1 = foundHelper || depth0.value;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "value", { hash: {} }); }
  buffer += escapeExpression(stack1) + "</option>\n            ";
  return buffer;}

  buffer += "<td class='code'>";
  foundHelper = helpers.name;
  stack1 = foundHelper || depth0.name;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "name", { hash: {} }); }
  buffer += escapeExpression(stack1) + "</td>\n<td>\n    <select class='parameter' name='";
  foundHelper = helpers.name;
  stack1 = foundHelper || depth0.name;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "name", { hash: {} }); }
  buffer += escapeExpression(stack1) + "'>\n        ";
  foundHelper = helpers.required;
  stack1 = foundHelper || depth0.required;
  stack2 = helpers['if'];
  tmp1 = self.program(1, program1, data);
  tmp1.hash = {};
  tmp1.fn = tmp1;
  tmp1.inverse = self.program(3, program3, data);
  stack1 = stack2.call(depth0, stack1, tmp1);
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "\n        ";
  foundHelper = helpers.allowableValues;
  stack1 = foundHelper || depth0.allowableValues;
  stack1 = (stack1 === null || stack1 === undefined || stack1 === false ? stack1 : stack1.descriptiveValues);
  stack2 = helpers.each;
  tmp1 = self.program(8, program8, data);
  tmp1.hash = {};
  tmp1.fn = tmp1;
  tmp1.inverse = self.noop;
  stack1 = stack2.call(depth0, stack1, tmp1);
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "\n    </select>\n</td>\n<td>";
  foundHelper = helpers.description;
  stack1 = foundHelper || depth0.description;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "description", { hash: {} }); }
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "</td>\n<td>";
  foundHelper = helpers.paramType;
  stack1 = foundHelper || depth0.paramType;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "paramType", { hash: {} }); }
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "</td>\n<td><span class=\"model-signature\"></span></td>\n";
  return buffer;});
})();

(function() {
  var template = Handlebars.template, templates = Handlebars.templates = Handlebars.templates || {};
templates['param_readonly'] = template(function (Handlebars,depth0,helpers,partials,data) {
  helpers = helpers || Handlebars.helpers;
  var buffer = "", stack1, stack2, foundHelper, tmp1, self=this, functionType="function", helperMissing=helpers.helperMissing, undef=void 0, escapeExpression=this.escapeExpression;

function program1(depth0,data) {
  
  var buffer = "", stack1;
  buffer += "\n        <textarea class='body-textarea' readonly='readonly' name='";
  foundHelper = helpers.name;
  stack1 = foundHelper || depth0.name;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "name", { hash: {} }); }
  buffer += escapeExpression(stack1) + "'>";
  foundHelper = helpers.defaultValue;
  stack1 = foundHelper || depth0.defaultValue;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "defaultValue", { hash: {} }); }
  buffer += escapeExpression(stack1) + "</textarea>\n    ";
  return buffer;}

function program3(depth0,data) {
  
  var buffer = "", stack1, stack2;
  buffer += "\n        ";
  foundHelper = helpers.defaultValue;
  stack1 = foundHelper || depth0.defaultValue;
  stack2 = helpers['if'];
  tmp1 = self.program(4, program4, data);
  tmp1.hash = {};
  tmp1.fn = tmp1;
  tmp1.inverse = self.program(6, program6, data);
  stack1 = stack2.call(depth0, stack1, tmp1);
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "\n    ";
  return buffer;}
function program4(depth0,data) {
  
  var buffer = "", stack1;
  buffer += "\n            ";
  foundHelper = helpers.defaultValue;
  stack1 = foundHelper || depth0.defaultValue;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "defaultValue", { hash: {} }); }
  buffer += escapeExpression(stack1) + "\n        ";
  return buffer;}

function program6(depth0,data) {
  
  
  return "\n            (empty)\n        ";}

  buffer += "<td class='code'>";
  foundHelper = helpers.name;
  stack1 = foundHelper || depth0.name;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "name", { hash: {} }); }
  buffer += escapeExpression(stack1) + "</td>\n<td>\n    ";
  foundHelper = helpers.isBody;
  stack1 = foundHelper || depth0.isBody;
  stack2 = helpers['if'];
  tmp1 = self.program(1, program1, data);
  tmp1.hash = {};
  tmp1.fn = tmp1;
  tmp1.inverse = self.program(3, program3, data);
  stack1 = stack2.call(depth0, stack1, tmp1);
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "\n</td>\n<td>";
  foundHelper = helpers.description;
  stack1 = foundHelper || depth0.description;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "description", { hash: {} }); }
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "</td>\n<td>";
  foundHelper = helpers.paramType;
  stack1 = foundHelper || depth0.paramType;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "paramType", { hash: {} }); }
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "</td>\n<td><span class=\"model-signature\"></span></td>\n";
  return buffer;});
})();

(function() {
  var template = Handlebars.template, templates = Handlebars.templates = Handlebars.templates || {};
templates['param_readonly_required'] = template(function (Handlebars,depth0,helpers,partials,data) {
  helpers = helpers || Handlebars.helpers;
  var buffer = "", stack1, stack2, foundHelper, tmp1, self=this, functionType="function", helperMissing=helpers.helperMissing, undef=void 0, escapeExpression=this.escapeExpression;

function program1(depth0,data) {
  
  var buffer = "", stack1;
  buffer += "\n        <textarea class='body-textarea'  readonly='readonly' placeholder='(required)' name='";
  foundHelper = helpers.name;
  stack1 = foundHelper || depth0.name;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "name", { hash: {} }); }
  buffer += escapeExpression(stack1) + "'>";
  foundHelper = helpers.defaultValue;
  stack1 = foundHelper || depth0.defaultValue;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "defaultValue", { hash: {} }); }
  buffer += escapeExpression(stack1) + "</textarea>\n    ";
  return buffer;}

function program3(depth0,data) {
  
  var buffer = "", stack1, stack2;
  buffer += "\n        ";
  foundHelper = helpers.defaultValue;
  stack1 = foundHelper || depth0.defaultValue;
  stack2 = helpers['if'];
  tmp1 = self.program(4, program4, data);
  tmp1.hash = {};
  tmp1.fn = tmp1;
  tmp1.inverse = self.program(6, program6, data);
  stack1 = stack2.call(depth0, stack1, tmp1);
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "\n    ";
  return buffer;}
function program4(depth0,data) {
  
  var buffer = "", stack1;
  buffer += "\n            ";
  foundHelper = helpers.defaultValue;
  stack1 = foundHelper || depth0.defaultValue;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "defaultValue", { hash: {} }); }
  buffer += escapeExpression(stack1) + "\n        ";
  return buffer;}

function program6(depth0,data) {
  
  
  return "\n            (empty)\n        ";}

  buffer += "<td class='code required'>";
  foundHelper = helpers.name;
  stack1 = foundHelper || depth0.name;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "name", { hash: {} }); }
  buffer += escapeExpression(stack1) + "</td>\n<td>\n    ";
  foundHelper = helpers.isBody;
  stack1 = foundHelper || depth0.isBody;
  stack2 = helpers['if'];
  tmp1 = self.program(1, program1, data);
  tmp1.hash = {};
  tmp1.fn = tmp1;
  tmp1.inverse = self.program(3, program3, data);
  stack1 = stack2.call(depth0, stack1, tmp1);
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "\n</td>\n<td>";
  foundHelper = helpers.description;
  stack1 = foundHelper || depth0.description;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "description", { hash: {} }); }
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "</td>\n<td>";
  foundHelper = helpers.paramType;
  stack1 = foundHelper || depth0.paramType;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "paramType", { hash: {} }); }
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "</td>\n<td><span class=\"model-signature\"></span></td>\n";
  return buffer;});
})();

(function() {
  var template = Handlebars.template, templates = Handlebars.templates = Handlebars.templates || {};
templates['param_required'] = template(function (Handlebars,depth0,helpers,partials,data) {
  helpers = helpers || Handlebars.helpers;
  var buffer = "", stack1, stack2, foundHelper, tmp1, self=this, functionType="function", helperMissing=helpers.helperMissing, undef=void 0, escapeExpression=this.escapeExpression;

function program1(depth0,data) {
  
  var buffer = "", stack1, stack2;
  buffer += "\n		";
  foundHelper = helpers.isFile;
  stack1 = foundHelper || depth0.isFile;
  stack2 = helpers['if'];
  tmp1 = self.program(2, program2, data);
  tmp1.hash = {};
  tmp1.fn = tmp1;
  tmp1.inverse = self.program(4, program4, data);
  stack1 = stack2.call(depth0, stack1, tmp1);
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "\n	";
  return buffer;}
function program2(depth0,data) {
  
  var buffer = "", stack1;
  buffer += "\n			<input type=\"file\" name='";
  foundHelper = helpers.name;
  stack1 = foundHelper || depth0.name;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "name", { hash: {} }); }
  buffer += escapeExpression(stack1) + "'/>\n		";
  return buffer;}

function program4(depth0,data) {
  
  var buffer = "", stack1, stack2;
  buffer += "\n			";
  foundHelper = helpers.defaultValue;
  stack1 = foundHelper || depth0.defaultValue;
  stack2 = helpers['if'];
  tmp1 = self.program(5, program5, data);
  tmp1.hash = {};
  tmp1.fn = tmp1;
  tmp1.inverse = self.program(7, program7, data);
  stack1 = stack2.call(depth0, stack1, tmp1);
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "\n		";
  return buffer;}
function program5(depth0,data) {
  
  var buffer = "", stack1;
  buffer += "\n				<textarea class='body-textarea' placeholder='(required)' name='";
  foundHelper = helpers.name;
  stack1 = foundHelper || depth0.name;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "name", { hash: {} }); }
  buffer += escapeExpression(stack1) + "'>";
  foundHelper = helpers.defaultValue;
  stack1 = foundHelper || depth0.defaultValue;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "defaultValue", { hash: {} }); }
  buffer += escapeExpression(stack1) + "</textarea>\n			";
  return buffer;}

function program7(depth0,data) {
  
  var buffer = "", stack1;
  buffer += "\n				<textarea class='body-textarea' placeholder='(required)' name='";
  foundHelper = helpers.name;
  stack1 = foundHelper || depth0.name;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "name", { hash: {} }); }
  buffer += escapeExpression(stack1) + "'></textarea>\n				<br />\n				<div class=\"content-type\" />\n			";
  return buffer;}

function program9(depth0,data) {
  
  var buffer = "", stack1, stack2;
  buffer += "\n		";
  foundHelper = helpers.isFile;
  stack1 = foundHelper || depth0.isFile;
  stack2 = helpers['if'];
  tmp1 = self.program(10, program10, data);
  tmp1.hash = {};
  tmp1.fn = tmp1;
  tmp1.inverse = self.program(12, program12, data);
  stack1 = stack2.call(depth0, stack1, tmp1);
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "\n	";
  return buffer;}
function program10(depth0,data) {
  
  var buffer = "", stack1;
  buffer += "\n			<input class='parameter' class='required' type='file' name='";
  foundHelper = helpers.name;
  stack1 = foundHelper || depth0.name;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "name", { hash: {} }); }
  buffer += escapeExpression(stack1) + "'/>\n		";
  return buffer;}

function program12(depth0,data) {
  
  var buffer = "", stack1, stack2;
  buffer += "\n			";
  foundHelper = helpers.defaultValue;
  stack1 = foundHelper || depth0.defaultValue;
  stack2 = helpers['if'];
  tmp1 = self.program(13, program13, data);
  tmp1.hash = {};
  tmp1.fn = tmp1;
  tmp1.inverse = self.program(15, program15, data);
  stack1 = stack2.call(depth0, stack1, tmp1);
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "\n		";
  return buffer;}
function program13(depth0,data) {
  
  var buffer = "", stack1;
  buffer += "\n				<input class='parameter required' minlength='1' name='";
  foundHelper = helpers.name;
  stack1 = foundHelper || depth0.name;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "name", { hash: {} }); }
  buffer += escapeExpression(stack1) + "' placeholder='(required)' type='text' value='";
  foundHelper = helpers.defaultValue;
  stack1 = foundHelper || depth0.defaultValue;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "defaultValue", { hash: {} }); }
  buffer += escapeExpression(stack1) + "'/>\n			";
  return buffer;}

function program15(depth0,data) {
  
  var buffer = "", stack1;
  buffer += "\n				<input class='parameter required' minlength='1' name='";
  foundHelper = helpers.name;
  stack1 = foundHelper || depth0.name;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "name", { hash: {} }); }
  buffer += escapeExpression(stack1) + "' placeholder='(required)' type='text' value=''/>\n			";
  return buffer;}

  buffer += "<td class='code required'>";
  foundHelper = helpers.name;
  stack1 = foundHelper || depth0.name;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "name", { hash: {} }); }
  buffer += escapeExpression(stack1) + "</td>\n<td>\n	";
  foundHelper = helpers.isBody;
  stack1 = foundHelper || depth0.isBody;
  stack2 = helpers['if'];
  tmp1 = self.program(1, program1, data);
  tmp1.hash = {};
  tmp1.fn = tmp1;
  tmp1.inverse = self.program(9, program9, data);
  stack1 = stack2.call(depth0, stack1, tmp1);
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "\n</td>\n<td>\n	<strong>";
  foundHelper = helpers.description;
  stack1 = foundHelper || depth0.description;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "description", { hash: {} }); }
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "</strong>\n</td>\n<td>";
  foundHelper = helpers.paramType;
  stack1 = foundHelper || depth0.paramType;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "paramType", { hash: {} }); }
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "</td>\n<td><span class=\"model-signature\"></span></td>\n";
  return buffer;});
})();

(function() {
  var template = Handlebars.template, templates = Handlebars.templates = Handlebars.templates || {};
templates['resource'] = template(function (Handlebars,depth0,helpers,partials,data) {
  helpers = helpers || Handlebars.helpers;
  var buffer = "", stack1, foundHelper, self=this, functionType="function", helperMissing=helpers.helperMissing, undef=void 0, escapeExpression=this.escapeExpression;


  buffer += "<div class='heading'>\n    <h2>\n        <a href='#!/";
  foundHelper = helpers.name;
  stack1 = foundHelper || depth0.name;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "name", { hash: {} }); }
  buffer += escapeExpression(stack1) + "' onclick=\"Docs.toggleEndpointListForResource('";
  foundHelper = helpers.name;
  stack1 = foundHelper || depth0.name;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "name", { hash: {} }); }
  buffer += escapeExpression(stack1) + "');\">/";
  foundHelper = helpers.name;
  stack1 = foundHelper || depth0.name;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "name", { hash: {} }); }
  buffer += escapeExpression(stack1) + "</a>\n    </h2>\n    <ul class='options'>\n        <li>\n            <a href='#!/";
  foundHelper = helpers.name;
  stack1 = foundHelper || depth0.name;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "name", { hash: {} }); }
  buffer += escapeExpression(stack1) + "' id='endpointListTogger_";
  foundHelper = helpers.name;
  stack1 = foundHelper || depth0.name;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "name", { hash: {} }); }
  buffer += escapeExpression(stack1) + "'\n               onclick=\"Docs.toggleEndpointListForResource('";
  foundHelper = helpers.name;
  stack1 = foundHelper || depth0.name;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "name", { hash: {} }); }
  buffer += escapeExpression(stack1) + "');\">Show/Hide</a>\n        </li>\n        <li>\n            <a href='#' onclick=\"Docs.collapseOperationsForResource('";
  foundHelper = helpers.name;
  stack1 = foundHelper || depth0.name;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "name", { hash: {} }); }
  buffer += escapeExpression(stack1) + "'); return false;\">\n                List Operations\n            </a>\n        </li>\n        <li>\n            <a href='#' onclick=\"Docs.expandOperationsForResource('";
  foundHelper = helpers.name;
  stack1 = foundHelper || depth0.name;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "name", { hash: {} }); }
  buffer += escapeExpression(stack1) + "'); return false;\">\n                Expand Operations\n            </a>\n        </li>\n        <li>\n            <a href='";
  foundHelper = helpers.url;
  stack1 = foundHelper || depth0.url;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "url", { hash: {} }); }
  buffer += escapeExpression(stack1) + "'>Raw</a>\n        </li>\n    </ul>\n</div>\n<ul class='endpoints' id='";
  foundHelper = helpers.name;
  stack1 = foundHelper || depth0.name;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "name", { hash: {} }); }
  buffer += escapeExpression(stack1) + "_endpoint_list' style='display:none'>\n\n</ul>\n";
  return buffer;});
})();

(function() {
  var template = Handlebars.template, templates = Handlebars.templates = Handlebars.templates || {};
templates['signature'] = template(function (Handlebars,depth0,helpers,partials,data) {
  helpers = helpers || Handlebars.helpers;
  var buffer = "", stack1, foundHelper, self=this, functionType="function", helperMissing=helpers.helperMissing, undef=void 0, escapeExpression=this.escapeExpression;


  buffer += "<div>\n<ul class=\"signature-nav\">\n    <li><a class=\"description-link\" href=\"#\">Model</a></li>\n    <li><a class=\"snippet-link\" href=\"#\">Model Schema</a></li>\n</ul>\n<div>\n\n<div class=\"signature-container\">\n    <div class=\"description\">\n        ";
  foundHelper = helpers.signature;
  stack1 = foundHelper || depth0.signature;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "signature", { hash: {} }); }
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "\n    </div>\n\n    <div class=\"snippet\">\n        <pre><code>";
  foundHelper = helpers.sampleJSON;
  stack1 = foundHelper || depth0.sampleJSON;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "sampleJSON", { hash: {} }); }
  buffer += escapeExpression(stack1) + "</code></pre>\n        <small class=\"notice\"></small>\n    </div>\n</div>\n\n";
  return buffer;});
})();

(function() {
  var template = Handlebars.template, templates = Handlebars.templates = Handlebars.templates || {};
templates['status_code'] = template(function (Handlebars,depth0,helpers,partials,data) {
  helpers = helpers || Handlebars.helpers;
  var buffer = "", stack1, foundHelper, self=this, functionType="function", helperMissing=helpers.helperMissing, undef=void 0, escapeExpression=this.escapeExpression;


  buffer += "<td width='15%' class='code'>";
  foundHelper = helpers.code;
  stack1 = foundHelper || depth0.code;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "code", { hash: {} }); }
  buffer += escapeExpression(stack1) + "</td>\n<td>";
  foundHelper = helpers.reason;
  stack1 = foundHelper || depth0.reason;
  if(typeof stack1 === functionType) { stack1 = stack1.call(depth0, { hash: {} }); }
  else if(stack1=== undef) { stack1 = helperMissing.call(depth0, "reason", { hash: {} }); }
  if(stack1 || stack1 === 0) { buffer += stack1; }
  buffer += "</td>\n\n";
  return buffer;});
})();



// Generated by CoffeeScript 1.4.0
(function() {
  var ContentTypeView, HeaderView, MainView, OperationView, ParameterView, ResourceView, SignatureView, StatusCodeView, SwaggerUi,
    __hasProp = {}.hasOwnProperty,
    __extends = function(child, parent) { for (var key in parent) { if (__hasProp.call(parent, key)) child[key] = parent[key]; } function ctor() { this.constructor = child; } ctor.prototype = parent.prototype; child.prototype = new ctor(); child.__super__ = parent.prototype; return child; };

  SwaggerUi = (function(_super) {

    __extends(SwaggerUi, _super);

    function SwaggerUi() {
      return SwaggerUi.__super__.constructor.apply(this, arguments);
    }

    SwaggerUi.prototype.dom_id = "swagger_ui";

    SwaggerUi.prototype.options = null;

    SwaggerUi.prototype.api = null;

    SwaggerUi.prototype.headerView = null;

    SwaggerUi.prototype.mainView = null;

    SwaggerUi.prototype.initialize = function(options) {
      var _this = this;
      if (options == null) {
        options = {};
      }
      if (options.dom_id != null) {
        this.dom_id = options.dom_id;
        delete options.dom_id;
      }
      if (!($('#' + this.dom_id) != null)) {
        $('body').append('<div id="' + this.dom_id + '"></div>');
      }
      this.options = options;
      this.options.success = function() {
        return _this.render();
      };
      this.options.progress = function(d) {
        return _this.showMessage(d);
      };
      this.options.failure = function(d) {
        return _this.onLoadFailure(d);
      };
      this.headerView = new HeaderView({
        el: $('#header')
      });
      return this.headerView.on('update-swagger-ui', function(data) {
        return _this.updateSwaggerUi(data);
      });
    };

    SwaggerUi.prototype.updateSwaggerUi = function(data) {
      this.options.discoveryUrl = data.discoveryUrl;
      this.options.apiKey = data.apiKey;
      return this.load();
    };

    SwaggerUi.prototype.load = function() {
      var _ref;
      if ((_ref = this.mainView) != null) {
        _ref.clear();
      }
      this.headerView.update(this.options.discoveryUrl, this.options.apiKey);
      return this.api = new SwaggerApi(this.options);
    };

    SwaggerUi.prototype.render = function() {
      var _this = this;
      this.showMessage('Finished Loading Resource Information. Rendering Swagger UI...');
      this.mainView = new MainView({
        model: this.api,
        el: $('#' + this.dom_id)
      }).render();
      this.showMessage();
      switch (this.options.docExpansion) {
        case "full":
          Docs.expandOperationsForResource('');
          break;
        case "list":
          Docs.collapseOperationsForResource('');
      }
      if (this.options.onComplete) {
        this.options.onComplete(this.api, this);
      }
      return setTimeout(function() {
        return Docs.shebang();
      }, 400);
    };

    SwaggerUi.prototype.showMessage = function(data) {
      if (data == null) {
        data = '';
      }
      $('#message-bar').removeClass('message-fail');
      $('#message-bar').addClass('message-success');
      return $('#message-bar').html(data);
    };

    SwaggerUi.prototype.onLoadFailure = function(data) {
      var val;
      if (data == null) {
        data = '';
      }
      $('#message-bar').removeClass('message-success');
      $('#message-bar').addClass('message-fail');
      val = $('#message-bar').html(data);
      if (this.options.onFailure != null) {
        this.options.onFailure(data);
      }
      return val;
    };

    return SwaggerUi;

  })(Backbone.Router);

  window.SwaggerUi = SwaggerUi;

  HeaderView = (function(_super) {

    __extends(HeaderView, _super);

    function HeaderView() {
      return HeaderView.__super__.constructor.apply(this, arguments);
    }

    HeaderView.prototype.events = {
      'click #show-pet-store-icon': 'showPetStore',
      'click #show-wordnik-dev-icon': 'showWordnikDev',
      'click #explore': 'showCustom',
      'keyup #input_baseUrl': 'showCustomOnKeyup',
      'keyup #input_apiKey': 'showCustomOnKeyup'
    };

    HeaderView.prototype.initialize = function() {};

    HeaderView.prototype.showPetStore = function(e) {
      return this.trigger('update-swagger-ui', {
        discoveryUrl: "http://petstore.swagger.wordnik.com/api/api-docs.json",
        apiKey: "special-key"
      });
    };

    HeaderView.prototype.showWordnikDev = function(e) {
      return this.trigger('update-swagger-ui', {
        discoveryUrl: "http://api.wordnik.com/v4/resources.json",
        apiKey: ""
      });
    };

    HeaderView.prototype.showCustomOnKeyup = function(e) {
      if (e.keyCode === 13) {
        return this.showCustom();
      }
    };

    HeaderView.prototype.showCustom = function(e) {
      if (e != null) {
        e.preventDefault();
      }
      return this.trigger('update-swagger-ui', {
        discoveryUrl: $('#input_baseUrl').val(),
        apiKey: $('#input_apiKey').val()
      });
    };

    HeaderView.prototype.update = function(url, apiKey, trigger) {
      if (trigger == null) {
        trigger = false;
      }
      $('#input_baseUrl').val(url);
      $('#input_apiKey').val(apiKey);
      if (trigger) {
        return this.trigger('update-swagger-ui', {
          discoveryUrl: url,
          apiKey: apiKey
        });
      }
    };

    return HeaderView;

  })(Backbone.View);

  MainView = (function(_super) {

    __extends(MainView, _super);

    function MainView() {
      return MainView.__super__.constructor.apply(this, arguments);
    }

    MainView.prototype.initialize = function() {};

    MainView.prototype.render = function() {
      var resource, _i, _len, _ref;
      $(this.el).html(Handlebars.templates.main(this.model));
      _ref = this.model.apisArray;
      for (_i = 0, _len = _ref.length; _i < _len; _i++) {
        resource = _ref[_i];
        this.addResource(resource);
      }
      return this;
    };

    MainView.prototype.addResource = function(resource) {
      var resourceView;
      resourceView = new ResourceView({
        model: resource,
        tagName: 'li',
        id: 'resource_' + resource.name,
        className: 'resource'
      });
      return $('#resources').append(resourceView.render().el);
    };

    MainView.prototype.clear = function() {
      return $(this.el).html('');
    };

    return MainView;

  })(Backbone.View);

  ResourceView = (function(_super) {

    __extends(ResourceView, _super);

    function ResourceView() {
      return ResourceView.__super__.constructor.apply(this, arguments);
    }

    ResourceView.prototype.initialize = function() {};

    ResourceView.prototype.render = function() {
      var operation, _i, _len, _ref;
      $(this.el).html(Handlebars.templates.resource(this.model));
      this.number = 0;
      _ref = this.model.operationsArray;
      for (_i = 0, _len = _ref.length; _i < _len; _i++) {
        operation = _ref[_i];
        this.addOperation(operation);
      }
      return this;
    };

    ResourceView.prototype.addOperation = function(operation) {
      var operationView;
      operation.number = this.number;
      operationView = new OperationView({
        model: operation,
        tagName: 'li',
        className: 'endpoint'
      });
      $('.endpoints', $(this.el)).append(operationView.render().el);
      return this.number++;
    };

    return ResourceView;

  })(Backbone.View);

  OperationView = (function(_super) {

    __extends(OperationView, _super);

    function OperationView() {
      return OperationView.__super__.constructor.apply(this, arguments);
    }

    OperationView.prototype.events = {
      'submit .sandbox': 'submitOperation',
      'click .submit': 'submitOperation',
      'click .response_hider': 'hideResponse',
      'click .toggleOperation': 'toggleOperationContent'
    };

    OperationView.prototype.initialize = function() {};

    OperationView.prototype.render = function() {
      var contentTypeModel, contentTypeView, isMethodSubmissionSupported, param, responseSignatureView, signatureModel, statusCode, _i, _j, _len, _len1, _ref, _ref1;
      isMethodSubmissionSupported = jQuery.inArray(this.model.httpMethod, this.model.supportedSubmitMethods()) >= 0;
      if (!isMethodSubmissionSupported) {
        this.model.isReadOnly = true;
      }
      $(this.el).html(Handlebars.templates.operation(this.model));
      if (this.model.responseClassSignature && this.model.responseClassSignature !== 'string') {
        signatureModel = {
          sampleJSON: this.model.responseSampleJSON,
          isParam: false,
          signature: this.model.responseClassSignature
        };
        responseSignatureView = new SignatureView({
          model: signatureModel,
          tagName: 'div'
        });
        $('.model-signature', $(this.el)).append(responseSignatureView.render().el);
      } else {
        $('.model-signature', $(this.el)).html(this.model.responseClass);
      }
      contentTypeModel = {
        isParam: false
      };
      if (this.model.supportedContentTypes) {
        contentTypeModel.produces = this.model.supportedContentTypes;
      }
      if (this.model.produces) {
        contentTypeModel.produces = this.model.produces;
      }
      contentTypeView = new ContentTypeView({
        model: contentTypeModel
      });
      $('.content-type', $(this.el)).append(contentTypeView.render().el);
      _ref = this.model.parameters;
      for (_i = 0, _len = _ref.length; _i < _len; _i++) {
        param = _ref[_i];
        this.addParameter(param);
      }
      _ref1 = this.model.errorResponses;
      for (_j = 0, _len1 = _ref1.length; _j < _len1; _j++) {
        statusCode = _ref1[_j];
        this.addStatusCode(statusCode);
      }
      return this;
    };

    OperationView.prototype.addParameter = function(param) {
      var paramView;
      paramView = new ParameterView({
        model: param,
        tagName: 'tr',
        readOnly: this.model.isReadOnly
      });
      return $('.operation-params', $(this.el)).append(paramView.render().el);
    };

    OperationView.prototype.addStatusCode = function(statusCode) {
      var statusCodeView;
      statusCodeView = new StatusCodeView({
        model: statusCode,
        tagName: 'tr'
      });
      return $('.operation-status', $(this.el)).append(statusCodeView.render().el);
    };

    OperationView.prototype.submitOperation = function(e) {
      var bodyParam, consumes, error_free, form, headerParams, invocationUrl, isFileUpload, isFormPost, map, o, obj, param, paramContentTypeField, responseContentTypeField, _i, _j, _k, _l, _len, _len1, _len2, _len3, _len4, _m, _ref, _ref1, _ref2, _ref3, _ref4,
        _this = this;
      if (e != null) {
        e.preventDefault();
      }
      form = $('.sandbox', $(this.el));
      error_free = true;
      form.find("input.required").each(function() {
        var _this = this;
        $(this).removeClass("error");
        if (jQuery.trim($(this).val()) === "") {
          $(this).addClass("error");
          $(this).wiggle({
            callback: function() {
              return $(_this).focus();
            }
          });
          return error_free = false;
        }
      });
      if (error_free) {
        map = {};
        _ref = form.serializeArray();
        for (_i = 0, _len = _ref.length; _i < _len; _i++) {
          o = _ref[_i];
          if ((o.value != null) && jQuery.trim(o.value).length > 0) {
            map[o.name] = o.value;
          }
        }
        isFileUpload = form.children().find('input[type~="file"]').size() !== 0;
        isFormPost = false;
        consumes = "application/json";
        if (this.model.consumes && this.model.consumes.length > 0) {
          consumes = this.model.consumes[0];
        } else {
          _ref1 = this.model.parameters;
          for (_j = 0, _len1 = _ref1.length; _j < _len1; _j++) {
            o = _ref1[_j];
            if (o.paramType === 'form') {
              isFormPost = true;
              consumes = false;
            }
          }
          if (isFileUpload) {
            consumes = false;
          } else if (this.model.httpMethod.toLowerCase() === "post" && isFormPost === false) {
            consumes = "application/json";
          }
        }
        if (isFileUpload) {
          bodyParam = new FormData();
          _ref2 = this.model.parameters;
          for (_k = 0, _len2 = _ref2.length; _k < _len2; _k++) {
            param = _ref2[_k];
            if ((param.paramType === 'body' || 'form') && param.name !== 'file' && param.name !== 'File' && (map[param.name] != null)) {
              bodyParam.append(param.name, map[param.name]);
            }
          }
          $.each(form.children().find('input[type~="file"]'), function(i, el) {
            return bodyParam.append($(el).attr('name'), el.files[0]);
          });
          console.log(bodyParam);
        } else if (isFormPost) {
          bodyParam = new FormData();
          _ref3 = this.model.parameters;
          for (_l = 0, _len3 = _ref3.length; _l < _len3; _l++) {
            param = _ref3[_l];
            if (map[param.name] != null) {
              bodyParam.append(param.name, map[param.name]);
            }
          }
        } else {
          bodyParam = null;
          _ref4 = this.model.parameters;
          for (_m = 0, _len4 = _ref4.length; _m < _len4; _m++) {
            param = _ref4[_m];
            if (param.paramType === 'body') {
              bodyParam = map[param.name];
            }
          }
        }
        log("bodyParam = " + bodyParam);
        headerParams = null;
        invocationUrl = this.model.supportHeaderParams() ? (headerParams = this.model.getHeaderParams(map), this.model.urlify(map, false)) : this.model.urlify(map, true);
        log('submitting ' + invocationUrl);
        $(".request_url", $(this.el)).html("<pre>" + invocationUrl + "</pre>");
        $(".response_throbber", $(this.el)).show();
        obj = {
          type: this.model.httpMethod,
          url: invocationUrl,
          headers: headerParams,
          data: bodyParam,
          contentType: consumes,
          dataType: 'json',
          processData: false,
          error: function(xhr, textStatus, error) {
            return _this.showErrorStatus(xhr, textStatus, error);
          },
          success: function(data) {
            return _this.showResponse(data);
          },
          complete: function(data) {
            return _this.showCompleteStatus(data);
          }
        };
        paramContentTypeField = $("td select[name=contentType]", $(this.el)).val();
        if (paramContentTypeField) {
          obj.contentType = paramContentTypeField;
        }
        log('content type = ' + obj.contentType);
        if (!(obj.data || (obj.type === 'GET' || obj.type === 'DELETE')) && obj.contentType === !"application/x-www-form-urlencoded") {
          obj.contentType = false;
        }
        log('content type is now = ' + obj.contentType);
        responseContentTypeField = $('.content > .content-type > div > select[name=contentType]', $(this.el)).val();
        if (responseContentTypeField) {
          obj.headers = obj.headers != null ? obj.headers : {};
          obj.headers.accept = responseContentTypeField;
        }
        jQuery.ajax(obj);
        return false;
      }
    };

    OperationView.prototype.hideResponse = function(e) {
      if (e != null) {
        e.preventDefault();
      }
      $(".response", $(this.el)).slideUp();
      return $(".response_hider", $(this.el)).fadeOut();
    };

    OperationView.prototype.showResponse = function(response) {
      var prettyJson;
      prettyJson = JSON.stringify(response, null, "\t").replace(/\n/g, "<br>");
      return $(".response_body", $(this.el)).html(escape(prettyJson));
    };

    OperationView.prototype.showErrorStatus = function(data) {
      return this.showStatus(data);
    };

    OperationView.prototype.showCompleteStatus = function(data) {
      return this.showStatus(data);
    };

    OperationView.prototype.formatXml = function(xml) {
      var contexp, formatted, indent, lastType, lines, ln, pad, reg, transitions, wsexp, _fn, _i, _len;
      reg = /(>)(<)(\/*)/g;
      wsexp = /[ ]*(.*)[ ]+\n/g;
      contexp = /(<.+>)(.+\n)/g;
      xml = xml.replace(reg, '$1\n$2$3').replace(wsexp, '$1\n').replace(contexp, '$1\n$2');
      pad = 0;
      formatted = '';
      lines = xml.split('\n');
      indent = 0;
      lastType = 'other';
      transitions = {
        'single->single': 0,
        'single->closing': -1,
        'single->opening': 0,
        'single->other': 0,
        'closing->single': 0,
        'closing->closing': -1,
        'closing->opening': 0,
        'closing->other': 0,
        'opening->single': 1,
        'opening->closing': 0,
        'opening->opening': 1,
        'opening->other': 1,
        'other->single': 0,
        'other->closing': -1,
        'other->opening': 0,
        'other->other': 0
      };
      _fn = function(ln) {
        var fromTo, j, key, padding, type, types, value;
        types = {
          single: Boolean(ln.match(/<.+\/>/)),
          closing: Boolean(ln.match(/<\/.+>/)),
          opening: Boolean(ln.match(/<[^!?].*>/))
        };
        type = ((function() {
          var _results;
          _results = [];
          for (key in types) {
            value = types[key];
            if (value) {
              _results.push(key);
            }
          }
          return _results;
        })())[0];
        type = type === void 0 ? 'other' : type;
        fromTo = lastType + '->' + type;
        lastType = type;
        padding = '';
        indent += transitions[fromTo];
        padding = ((function() {
          var _j, _ref, _results;
          _results = [];
          for (j = _j = 0, _ref = indent; 0 <= _ref ? _j < _ref : _j > _ref; j = 0 <= _ref ? ++_j : --_j) {
            _results.push('  ');
          }
          return _results;
        })()).join('');
        if (fromTo === 'opening->closing') {
          return formatted = formatted.substr(0, formatted.length - 1) + ln + '\n';
        } else {
          return formatted += padding + ln + '\n';
        }
      };
      for (_i = 0, _len = lines.length; _i < _len; _i++) {
        ln = lines[_i];
        _fn(ln);
      }
      return formatted;
    };

    OperationView.prototype.showStatus = function(data) {
      var code, pre, response_body;
      try {
        code = $('<code />').text(JSON.stringify(JSON.parse(data.responseText), null, 2));
        pre = $('<pre class="json" />').append(code);
      } catch (error) {
        code = $('<code />').text(this.formatXml(data.responseText));
        pre = $('<pre class="xml" />').append(code);
      }
      response_body = pre;
      $(".response_code", $(this.el)).html("<pre>" + data.status + "</pre>");
      $(".response_body", $(this.el)).html(response_body);
      $(".response_headers", $(this.el)).html("<pre>" + data.getAllResponseHeaders() + "</pre>");
      $(".response", $(this.el)).slideDown();
      $(".response_hider", $(this.el)).show();
      $(".response_throbber", $(this.el)).hide();
      return hljs.highlightBlock($('.response_body', $(this.el))[0]);
    };

    OperationView.prototype.toggleOperationContent = function() {
      var elem;
      elem = $('#' + Docs.escapeResourceName(this.model.resourceName) + "_" + this.model.nickname + "_" + this.model.httpMethod + "_" + this.model.number + "_content");
      if (elem.is(':visible')) {
        return Docs.collapseOperation(elem);
      } else {
        return Docs.expandOperation(elem);
      }
    };

    return OperationView;

  })(Backbone.View);

  StatusCodeView = (function(_super) {

    __extends(StatusCodeView, _super);

    function StatusCodeView() {
      return StatusCodeView.__super__.constructor.apply(this, arguments);
    }

    StatusCodeView.prototype.initialize = function() {};

    StatusCodeView.prototype.render = function() {
      var template;
      template = this.template();
      $(this.el).html(template(this.model));
      return this;
    };

    StatusCodeView.prototype.template = function() {
      return Handlebars.templates.status_code;
    };

    return StatusCodeView;

  })(Backbone.View);

  ParameterView = (function(_super) {

    __extends(ParameterView, _super);

    function ParameterView() {
      return ParameterView.__super__.constructor.apply(this, arguments);
    }

    ParameterView.prototype.initialize = function() {};

    ParameterView.prototype.render = function() {
      var contentTypeModel, contentTypeView, signatureModel, signatureView, template;
      if (this.model.paramType === 'body') {
        this.model.isBody = true;
      }
      if (this.model.dataType === 'file') {
        this.model.isFile = true;
      }
      template = this.template();
      $(this.el).html(template(this.model));
      signatureModel = {
        sampleJSON: this.model.sampleJSON,
        isParam: true,
        signature: this.model.signature
      };
      if (this.model.sampleJSON) {
        signatureView = new SignatureView({
          model: signatureModel,
          tagName: 'div'
        });
        $('.model-signature', $(this.el)).append(signatureView.render().el);
      } else {
        $('.model-signature', $(this.el)).html(this.model.signature);
      }
      contentTypeModel = {
        isParam: false
      };
      if (this.model.supportedContentTypes) {
        contentTypeModel.produces = this.model.supportedContentTypes;
      }
      if (this.model.produces) {
        contentTypeModel.produces = this.model.produces;
      }
      contentTypeView = new ContentTypeView({
        model: contentTypeModel
      });
      $('.content-type', $(this.el)).append(contentTypeView.render().el);
      return this;
    };

    ParameterView.prototype.template = function() {
      if (this.model.isList) {
        return Handlebars.templates.param_list;
      } else {
        if (this.options.readOnly) {
          if (this.model.required) {
            return Handlebars.templates.param_readonly_required;
          } else {
            return Handlebars.templates.param_readonly;
          }
        } else {
          if (this.model.required) {
            return Handlebars.templates.param_required;
          } else {
            return Handlebars.templates.param;
          }
        }
      }
    };

    return ParameterView;

  })(Backbone.View);

  SignatureView = (function(_super) {

    __extends(SignatureView, _super);

    function SignatureView() {
      return SignatureView.__super__.constructor.apply(this, arguments);
    }

    SignatureView.prototype.events = {
      'click a.description-link': 'switchToDescription',
      'click a.snippet-link': 'switchToSnippet',
      'mousedown .snippet': 'snippetToTextArea'
    };

    SignatureView.prototype.initialize = function() {};

    SignatureView.prototype.render = function() {
      var template;
      template = this.template();
      $(this.el).html(template(this.model));
      this.switchToDescription();
      this.isParam = this.model.isParam;
      if (this.isParam) {
        $('.notice', $(this.el)).text('Click to set as parameter value');
      }
      return this;
    };

    SignatureView.prototype.template = function() {
      return Handlebars.templates.signature;
    };

    SignatureView.prototype.switchToDescription = function(e) {
      if (e != null) {
        e.preventDefault();
      }
      $(".snippet", $(this.el)).hide();
      $(".description", $(this.el)).show();
      $('.description-link', $(this.el)).addClass('selected');
      return $('.snippet-link', $(this.el)).removeClass('selected');
    };

    SignatureView.prototype.switchToSnippet = function(e) {
      if (e != null) {
        e.preventDefault();
      }
      $(".description", $(this.el)).hide();
      $(".snippet", $(this.el)).show();
      $('.snippet-link', $(this.el)).addClass('selected');
      return $('.description-link', $(this.el)).removeClass('selected');
    };

    SignatureView.prototype.snippetToTextArea = function(e) {
      var textArea;
      if (this.isParam) {
        if (e != null) {
          e.preventDefault();
        }
        textArea = $('textarea', $(this.el.parentNode.parentNode.parentNode));
        if ($.trim(textArea.val()) === '') {
          return textArea.val(this.model.sampleJSON);
        }
      }
    };

    return SignatureView;

  })(Backbone.View);

  ContentTypeView = (function(_super) {

    __extends(ContentTypeView, _super);

    function ContentTypeView() {
      return ContentTypeView.__super__.constructor.apply(this, arguments);
    }

    ContentTypeView.prototype.initialize = function() {};

    ContentTypeView.prototype.render = function() {
      var template;
      template = this.template();
      $(this.el).html(template(this.model));
      this.isParam = this.model.isParam;
      if (this.isParam) {
        $('label[for=contentType]', $(this.el)).text('Parameter content type:');
      } else {
        $('label[for=contentType]', $(this.el)).text('Response Content Type');
      }
      return this;
    };

    ContentTypeView.prototype.template = function() {
      return Handlebars.templates.content_type;
    };

    return ContentTypeView;

  })(Backbone.View);

}).call(this);
