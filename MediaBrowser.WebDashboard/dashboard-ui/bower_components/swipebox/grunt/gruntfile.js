module.exports = function(grunt) {

	// don't forget to update the version in the package.json and bower.json file

	// load dependencies
	require('load-grunt-tasks')(grunt);
	
	grunt.initConfig({

		version: grunt.file.readJSON('package.json').version,
		pkg: grunt.file.readJSON('package.json'),

		// notify cross-OS - see https://github.com/dylang/grunt-notify
		notify: {
			
			scss: {
				options: {
					title: 'SCSS compiled',
					message: 'CSS is in the hood'
				}
			},

			js: {
				options: {
					title: 'JS checked and minified',
					message: 'JS is all good'
				}
			},

			dist: {
				options: {
					title: 'Project Compiled',
					message: 'All good'
				}
			}
		},

		// compile scss
		sass: {

			dist:{
				options:{
					style: 'expanded'
				},

				files:{
					'../src/css/swipebox.css': '../scss/swipebox.scss',
				}
			},

			demo:{
				options:{
					style: 'compressed'
				},

				files:{
					'../demo/style.css': '../demo/scss/style.scss',
				}
			}

		},

		// https://github.com/nDmitry/grunt-autoprefixer
		autoprefixer: {
			options: {
				browsers: ['last 3 versions', 'bb 10', 'android 3']
			},
			no_dest: {
				src: '../src/css/swipebox.css',
			}
		},

		// minify css asset files
		cssmin: {
			minify: {
				expand: true,
				cwd: '../src/css/',
				src: ['*.css', '!*.min.css'],
				dest: '../src/css/',
				ext: '.min.css'
			}
		},

		// chech our JS
		jshint: {
			options : {
				jshintrc : '.jshintrc'
			},
			all: [ '../src/js/jquery.swipebox.js' ]
		},

		// minify JS
		uglify: {

			options:{
				banner : '/*! Swipebox v<%= version %> | Constantin Saguin csag.co | MIT License | github.com/brutaldesign/swipebox */\n'
			},

			admin: {
				files: {
					'../src/js/jquery.swipebox.min.js': [ '../src/js/jquery.swipebox.js']
				}
			}
		},

		// watch it live
		watch: {
			js: {                       
				files: [ '../src/js/*.js' ],
				tasks: [
					'jshint',
					'uglify',
					'notify:js'
				],
			},
			scss: {

				files: ['../scss/*.scss', '../demo/scss/*.scss'],
				tasks: [
					'sass',
					'autoprefixer',
					'cssmin',
					'notify:scss'
				],
			},

			css: {
				files: ['*.css']
			},

			livereload: {
				files: [ '../src/css/*.css', '../demo/*.css' ],
				options: { livereload: true }
			}
		},

		
	} ); // end init config

	/**
	 * Default task
	 */
	grunt.registerTask( 'default', [
		'sass:dist',
		'autoprefixer',
		'cssmin',
		'jshint',
		'uglify',
		'sass:demo',
		'notify:dist'
	] );

	/**
	 * Dev task
	 *
	 * The main tasks for development
	 *
	 */
	grunt.registerTask( 'dev', [
		'sass:dist',
		'autoprefixer',
		'cssmin',
		'jshint',
		'uglify',
		'sass:demo',
		'watch'
	] );
};