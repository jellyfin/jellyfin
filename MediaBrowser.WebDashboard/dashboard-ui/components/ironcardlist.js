define(['iron-list', 'lazyload-image'], function () {

    function getTemplate(scrollTarget) {

        var html = '';

        html += '<template is="dom-bind">\
	<iron-list as="item" id="ironList" scroll-target="' + scrollTarget + '" max-physical-count="60" style="width:96%;" grid>\
		<template>\
			<div class$="{{item.elemClass}}" data-action$="{{item.defaultAction}}">\
				<div class$="{{item.cardBoxClass}}">\
					<div class="cardScalable">\
						<div class="cardPadder"></div>\
						<a onclick$="{{item.onclick}}" class$="{{item.anchorClass}}" href$="{{item.href}}">\
							<img class$="{{item.imageClass}}" is="lazyload-image" src$="{{item.imgUrl}}" />\
						</a>\
					</div>\
					<!--cardFooter will be here-->\
				</div>\
			</div>\
		</template>\
	</iron-list>\
</template>';

        return Promise.resolve(html);
    }

    return {
        getTemplate: getTemplate
    };

});