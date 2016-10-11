gulp = require 'gulp'
coffee = require 'gulp-coffee'
browserify = require 'browserify'
runSequence = require 'run-sequence'
source = require 'vinyl-source-stream'
del = require 'del'
closureCompiler = require 'gulp-closure-compiler'

gulp.task 'coffee', ->
  gulp.src('*.coffee')
    .pipe(coffee())
    .pipe(gulp.dest('.'))

  gulp.src('src/*.coffee')
    .pipe(coffee())
    .pipe(gulp.dest('src'))

gulp.task 'browserify', ->
  browserify(
    entries: 'src/Vibrant.js'
  )
    .bundle()
    .pipe(source('Vibrant.js'))
    .pipe(gulp.dest('dist'))

gulp.task 'default', ->
  runSequence 'coffee', 'browserify', 'cleanup', 'closure'

gulp.task 'cleanup', (cb) ->
  del ['src/Vibrant.js'], cb

gulp.task 'closure', ->
  gulp.src('dist/Vibrant.js')
  .pipe(closureCompiler(
      compilerPath: 'bower_components/closure-compiler/lib/vendor/compiler.jar',
      fileName: 'Vibrant.min.js',
      continueWithWarnings: true
    ))
  .pipe(gulp.dest('dist'));

gulp.task 'watch', ->
  gulp.watch ['**/*.coffee'], ['default']