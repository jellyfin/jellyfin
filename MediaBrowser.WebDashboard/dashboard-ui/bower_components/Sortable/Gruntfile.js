module.exports = function (grunt) {
	'use strict';

	grunt.initConfig({
		pkg: grunt.file.readJSON('package.json'),

		version: {
			js: {
				src: ['<%= pkg.exportName %>.js', '*.json']
			},
			cdn: {
				options: {
					prefix: '(cdnjs\\.cloudflare\\.com\\/ajax\\/libs\\/Sortable|cdn\\.jsdelivr\\.net\\/sortable)\\/',
					replace: '[0-9\\.]+'
				},
				src: ['README.md']
			}
		},

		jshint: {
			all: ['*.js', '!*.min.js'],

			options: {
				jshintrc: true
			}
		},

		uglify: {
			options: {
				banner: '/*! <%= pkg.exportName %> <%= pkg.version %> - <%= pkg.license %> | <%= pkg.repository.url %> */\n'
			},
			dist: {
				files: {
					'<%= pkg.exportName %>.min.js': ['<%= pkg.exportName %>.js']
				}
			},
			jquery: {
				files: {}
			}
		},

		exec: {
			'meteor-test': {
				command: 'meteor/runtests.sh'
			},
			'meteor-publish': {
				command: 'meteor/publish.sh'
			}
		},

		jquery: {}
	});


	grunt.registerTask('jquery', function (exportName, uglify) {
		if (exportName == 'min') {
			exportName = null;
			uglify = 'min';
		}

		if (!exportName) {
			exportName = 'sortable';
		}

		var fs = require('fs'),
			filename = 'jquery.fn.' + exportName + '.js';

		grunt.log.oklns(filename);

		fs.writeFileSync(
			filename,
			(fs.readFileSync('jquery.binding.js') + '')
				.replace('$.fn.sortable', '$.fn.' + exportName)
				.replace('/* CODE */',
					(fs.readFileSync('Sortable.js') + '')
						.replace(/^[\s\S]*?function[\s\S]*?(var[\s\S]+)\/\/\s+Export[\s\S]+/, '$1')
				)
		);

		if (uglify) {
			var opts = {};

			opts['jquery.fn.' + exportName + '.min.js'] = filename;
			grunt.config.set('uglify.jquery.files', opts);

			grunt.task.run('uglify:jquery');
		}
	});


	grunt.loadNpmTasks('grunt-version');
	grunt.loadNpmTasks('grunt-contrib-jshint');
	grunt.loadNpmTasks('grunt-contrib-uglify');
	grunt.loadNpmTasks('grunt-exec');

	// Meteor tasks
	grunt.registerTask('meteor-test', 'exec:meteor-test');
	grunt.registerTask('meteor-publish', 'exec:meteor-publish');
	grunt.registerTask('meteor', ['meteor-test', 'meteor-publish']);

	grunt.registerTask('tests', ['jshint']);
	grunt.registerTask('default', ['tests', 'version', 'uglify:dist']);
};
