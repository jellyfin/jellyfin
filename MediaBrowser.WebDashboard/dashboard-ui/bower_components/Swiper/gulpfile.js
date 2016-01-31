(function(){
    'use strict';
    var gulp = require('gulp'),
        connect = require('gulp-connect'),
        open = require('gulp-open'),
        less = require('gulp-less'),
        rename = require('gulp-rename'),
        header = require('gulp-header'),
        path = require('path'),
        uglify = require('gulp-uglify'),
        sourcemaps = require('gulp-sourcemaps'),
        minifyCSS = require('gulp-minify-css'),
        tap = require('gulp-tap'),
        concat = require('gulp-concat'),
        jshint = require('gulp-jshint'),
        stylish = require('jshint-stylish'),
        fs = require('fs'),
        paths = {
            root: './',
            build: {
                root: 'build/',
                styles: 'build/css/',
                scripts: 'build/js/'
            },
            dist: {
                root: 'dist/',
                styles: 'dist/css/',
                scripts: 'dist/js/'
            },
            playground: {
                root: 'playground/'
            },
            source: {
                root: 'src/',
                styles: 'src/less/',
                scripts: 'src/js/*.js'
            },
        },
        swiper = {
            filename: 'swiper',
            jsFiles: [
                'src/js/wrap-start.js',
                'src/js/swiper-intro.js',
                'src/js/core.js',
                'src/js/effects.js',
                'src/js/lazy-load.js',
                'src/js/scrollbar.js',
                'src/js/controller.js',
                'src/js/hashnav.js',
                'src/js/keyboard.js',
                'src/js/mousewheel.js',
                'src/js/parallax.js',
                'src/js/plugins.js',
                'src/js/emitter.js',
                'src/js/a11y.js',
                'src/js/init.js',
                'src/js/swiper-outro.js',
                'src/js/swiper-proto.js',
                'src/js/dom.js',
                'src/js/get-dom-lib.js',
                'src/js/dom-plugins.js',
                'src/js/wrap-end.js',
                'src/js/amd.js'
            ],
            jQueryFiles : [
                'src/js/wrap-start.js',
                'src/js/swiper-intro.js',
                'src/js/core.js',
                'src/js/effects.js',
                'src/js/lazy-load.js',
                'src/js/scrollbar.js',
                'src/js/controller.js',
                'src/js/hashnav.js',
                'src/js/keyboard.js',
                'src/js/mousewheel.js',
                'src/js/parallax.js',
                'src/js/plugins.js',
                'src/js/emitter.js',
                'src/js/a11y.js',
                'src/js/init.js',
                'src/js/swiper-outro.js',
                'src/js/swiper-proto.js',
                'src/js/get-dom-lib.js',
                'src/js/dom-plugins.js',
                'src/js/wrap-end.js',
                'src/js/amd.js'
            ],
            jQueryUMDFiles : [
                'src/js/wrap-start-umd.js',
                'src/js/swiper-intro.js',
                'src/js/core.js',
                'src/js/effects.js',
                'src/js/lazy-load.js',
                'src/js/scrollbar.js',
                'src/js/controller.js',
                'src/js/hashnav.js',
                'src/js/keyboard.js',
                'src/js/mousewheel.js',
                'src/js/parallax.js',
                'src/js/plugins.js',
                'src/js/emitter.js',
                'src/js/a11y.js',
                'src/js/init.js',
                'src/js/swiper-outro.js',
                'src/js/swiper-proto.js',
                'src/js/get-jquery.js',
                'src/js/dom-plugins.js',
                'src/js/wrap-end-umd.js',
            ],
            Framework7Files : [
                'src/js/swiper-intro-f7.js',
                'src/js/core.js',
                'src/js/effects.js',
                'src/js/lazy-load.js',
                'src/js/scrollbar.js',
                'src/js/controller.js',
                'src/js/parallax.js',
                'src/js/plugins.js',
                'src/js/emitter.js',
                'src/js/a11y.js',
                'src/js/init.js',
                'src/js/swiper-outro.js',
                'src/js/swiper-proto.js',
            ],
            pkg: require('./bower.json'),
            banner: [
                '/**',
                ' * Swiper <%= pkg.version %>',
                ' * <%= pkg.description %>',
                ' * ',
                ' * <%= pkg.homepage %>',
                ' * ',
                ' * Copyright <%= date.year %>, <%= pkg.author %>',
                ' * The iDangero.us',
                ' * http://www.idangero.us/',
                ' * ',
                ' * Licensed under <%= pkg.license.join(" & ") %>',
                ' * ',
                ' * Released on: <%= date.month %> <%= date.day %>, <%= date.year %>',
                ' */',
                ''].join('\n'),
            date: {
                year: new Date().getFullYear(),
                month: ('January February March April May June July August September October November December').split(' ')[new Date().getMonth()],
                day: new Date().getDate()
            }
        };

    function addJSIndent (file, t, minusIndent) {
        var addIndent = '        ';
        var filename = file.path.split('src/js/')[1];
        if (['wrap-start.js', 'wrap-start-umd.js', 'wrap-end.js', 'wrap-end-umd.js', 'amd.js'].indexOf(filename) !== -1) {
            addIndent = '';
        }
        if (filename === 'swiper-intro.js' || filename === 'swiper-intro-f7.js' || filename === 'swiper-outro.js' || filename === 'dom.js' || filename === 'get-dom-lib.js' || filename === 'get-jquery.js' || filename === 'dom-plugins.js' || filename === 'swiper-proto.js') addIndent = '    ';
        if (minusIndent) {
            addIndent = addIndent.substring(4);
        }
        if (addIndent !== '') {
            var fileLines = fs.readFileSync(file.path).toString().split('\n');
            var newFileContents = '';
            for (var i = 0; i < fileLines.length; i++) {
                newFileContents += addIndent + fileLines[i] + (i === fileLines.length ? '' : '\n');
            }
            file.contents = new Buffer(newFileContents);
        }
    }
    gulp.task('scripts', function (cb) {
        gulp.src(swiper.jsFiles)
            .pipe(tap(function (file, t){
                addJSIndent (file, t);
            }))
            .pipe(concat(swiper.filename + '.js'))
            .pipe(header(swiper.banner, { pkg : swiper.pkg, date: swiper.date } ))
            .pipe(gulp.dest(paths.build.scripts))

            .pipe(jshint())
            .pipe(jshint.reporter(stylish));
        gulp.src(swiper.jQueryFiles)
            .pipe(tap(function (file, t){
                addJSIndent (file, t);
            }))
            .pipe(concat(swiper.filename + '.jquery.js'))
            .pipe(header(swiper.banner, { pkg : swiper.pkg, date: swiper.date } ))
            .pipe(gulp.dest(paths.build.scripts));
        gulp.src(swiper.jQueryUMDFiles)
            .pipe(tap(function (file, t){
                addJSIndent (file, t);
            }))
            .pipe(concat(swiper.filename + '.jquery.umd.js'))
            .pipe(header(swiper.banner, { pkg : swiper.pkg, date: swiper.date } ))
            .pipe(gulp.dest(paths.build.scripts));
        gulp.src(swiper.Framework7Files)
            .pipe(tap(function (file, t){
                addJSIndent (file, t, true);
            }))
            .pipe(concat(swiper.filename + '.framework7.js'))
            .pipe(header(swiper.banner, { pkg : swiper.pkg, date: swiper.date } ))
            .pipe(gulp.dest(paths.build.scripts))
            .pipe(connect.reload());
        cb();
    });
    gulp.task('styles', function (cb) {

        gulp.src(paths.source.styles + 'swiper.less')
            .pipe(less({
                paths: [ path.join(__dirname, 'less', 'includes') ]
            }))
            .pipe(header(swiper.banner, { pkg : swiper.pkg, date: swiper.date }))
            .pipe(rename(function(path) {
                path.basename = swiper.filename;
            }))
            .pipe(gulp.dest(paths.build.styles))
            .pipe(connect.reload());

        gulp.src([
                paths.source.styles + 'core.less',
                paths.source.styles + 'navigation-f7.less',
                paths.source.styles + 'effects.less',
                paths.source.styles + 'scrollbar.less',
                paths.source.styles + 'preloader-f7.less',
            ])
            .pipe(concat(swiper.filename + '.framework7.less'))
            .pipe(header('/* === Swiper === */\n'))
            .pipe(gulp.dest(paths.build.styles));
        cb();
    });
    gulp.task('build', ['scripts', 'styles'], function (cb) {
        cb();
    });

    gulp.task('dist', function () {
        gulp.src([paths.build.scripts + swiper.filename + '.js'])
            .pipe(gulp.dest(paths.dist.scripts))
            .pipe(sourcemaps.init())
            .pipe(uglify())
            .pipe(header(swiper.banner, { pkg : swiper.pkg, date: swiper.date }))
            .pipe(rename(function(path) {
                path.basename = swiper.filename + '.min';
            }))
            .pipe(sourcemaps.write('./maps'))
            .pipe(gulp.dest(paths.dist.scripts));

        gulp.src([paths.build.scripts + swiper.filename + '.jquery.js'])
            .pipe(gulp.dest(paths.dist.scripts))
            .pipe(sourcemaps.init())
            .pipe(uglify())
            .pipe(header(swiper.banner, { pkg : swiper.pkg, date: swiper.date } ))
            .pipe(rename(function(path) {
                path.basename = swiper.filename + '.jquery.min';
            }))
            .pipe(sourcemaps.write('./maps'))
            .pipe(gulp.dest(paths.dist.scripts));

        gulp.src([paths.build.scripts + swiper.filename + '.jquery.umd.js'])
            .pipe(gulp.dest(paths.dist.scripts))
            .pipe(sourcemaps.init())
            .pipe(uglify())
            .pipe(header(swiper.banner, { pkg : swiper.pkg, date: swiper.date } ))
            .pipe(rename(function(path) {
                path.basename = swiper.filename + '.jquery.umd.min';
            }))
            .pipe(sourcemaps.write('./maps'))
            .pipe(gulp.dest(paths.dist.scripts));

        gulp.src(paths.build.styles + '*.css')
            .pipe(gulp.dest(paths.dist.styles))
            .pipe(minifyCSS({
                advanced: false,
                aggressiveMerging: false,
            }))
            .pipe(header(swiper.banner, { pkg : swiper.pkg, date: swiper.date }))
            .pipe(rename(function(path) {
                path.basename = swiper.filename + '.min';
            }))
            .pipe(gulp.dest(paths.dist.styles));
    });

    gulp.task('watch', function () {
        gulp.watch(paths.source.scripts, [ 'scripts' ]);
        gulp.watch(paths.source.styles + '*.less', [ 'styles' ]);
    });

    gulp.task('connect', function () {
        return connect.server({
            root: [ paths.root ],
            livereload: true,
            port:'3000'
        });
    });

    gulp.task('open', function () {
        return gulp.src(paths.playground.root + 'index.html').pipe(open({ uri: 'http://localhost:3000/' + paths.playground.root + 'index.html'}));
    });

    gulp.task('server', [ 'watch', 'connect', 'open' ]);

    gulp.task('default', [ 'server' ]);
})();
