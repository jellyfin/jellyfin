/*global requirejs: false*/

// -------------------------- pkgd -------------------------- //

/*
requirejs( [ '../../dist/masonry.pkgd' ], function( Masonry ) {
  new Masonry( document.querySelector('#basic') );
});
// */

// -------------------------- bower -------------------------- //

/*
requirejs.config({
  baseUrl: '../bower_components'
});

requirejs( [ '../masonry' ], function( Masonry ) {
  new Masonry( document.querySelector('#basic') );
});
// */

// -------------------------- pkgd & jQuery -------------------------- //

// /*
requirejs.config({
  paths: {
    jquery: '../../bower_components/jquery/dist/jquery'
  }
});

requirejs( [ 'require', 'jquery', '../../dist/masonry.pkgd' ],
  function( require, $, Masonry ) {
    require( [
      'jquery-bridget/jquery.bridget'
    ],
    function() {
      $.bridget( 'masonry', Masonry );
      $('#basic').masonry({
        columnWidth: 60
      });
    }
  );
});
// */

// -------------------------- bower & jQuery -------------------------- //

/*
requirejs.config({
  baseUrl: '../bower_components',
  paths: {
    jquery: 'jquery/dist/jquery'
  }
});

requirejs( [
    'jquery',
    '../masonry',
    'jquery-bridget/jquery.bridget'
  ],
  function( $, Masonry )  {
    $.bridget( 'masonry', Masonry );
    $('#basic').masonry({
      columnWidth: 60
    });
  }
);
// */