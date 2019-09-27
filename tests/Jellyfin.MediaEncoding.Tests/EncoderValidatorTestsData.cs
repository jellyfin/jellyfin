namespace Jellyfin.MediaEncoding.Tests
{
    internal static class EncoderValidatorTestsData
    {
        public const string FFmpegV42Output = @"ffmpeg version n4.2 Copyright (c) 2000-2019 the FFmpeg developers
built with gcc 9.1.0 (GCC)
configuration: --prefix=/usr --disable-debug --disable-static --disable-stripping --enable-fontconfig --enable-gmp --enable-gnutls --enable-gpl --enable-ladspa --enable-libaom --enable-libass --enable-libbluray --enable-libdav1d --enable-libdrm --enable-libfreetype --enable-libfribidi --enable-libgsm --enable-libiec61883 --enable-libjack --enable-libmodplug --enable-libmp3lame --enable-libopencore_amrnb --enable-libopencore_amrwb --enable-libopenjpeg --enable-libopus --enable-libpulse --enable-libsoxr --enable-libspeex --enable-libssh --enable-libtheora --enable-libv4l2 --enable-libvidstab --enable-libvorbis --enable-libvpx --enable-libwebp --enable-libx264 --enable-libx265 --enable-libxcb --enable-libxml2 --enable-libxvid --enable-nvdec --enable-nvenc --enable-omx --enable-shared --enable-version3
libavutil      56. 31.100 / 56. 31.100
libavcodec     58. 54.100 / 58. 54.100
libavformat    58. 29.100 / 58. 29.100
libavdevice    58.  8.100 / 58.  8.100
libavfilter     7. 57.100 /  7. 57.100
libswscale      5.  5.100 /  5.  5.100
libswresample   3.  5.100 /  3.  5.100
libpostproc    55.  5.100 / 55.  5.100
";

    public const string FFmpegV404Output = @"ffmpeg version 4.0.4 Copyright (c) 2000-2019 the FFmpeg developers
built with gcc 8 (Debian 8.3.0-6)
configuration: --toolchain=hardened --prefix=/usr --target-os=linux --enable-cross-compile --extra-cflags=--static --enable-gpl --enable-static --disable-doc --disable-ffplay --disable-shared --disable-libxcb --disable-sdl2 --disable-xlib --enable-libfontconfig --enable-fontconfig --enable-gmp --enable-gnutls --enable-libass --enable-libbluray --enable-libdrm --enable-libfreetype --enable-libfribidi --enable-libmp3lame --enable-libopus --enable-libtheora --enable-libvorbis --enable-libwebp --enable-libx264 --enable-libx265 --enable-libzvbi --enable-omx --enable-omx-rpi --enable-version3 --enable-vaapi --enable-vdpau --arch=amd64 --enable-nvenc --enable-nvdec
libavutil      56. 14.100 / 56. 14.100
libavcodec     58. 18.100 / 58. 18.100
libavformat    58. 12.100 / 58. 12.100
libavdevice    58.  3.100 / 58.  3.100
libavfilter     7. 16.100 /  7. 16.100
libswscale      5.  1.100 /  5.  1.100
libswresample   3.  1.100 /  3.  1.100
libpostproc    55.  1.100 / 55.  1.100
";
    }
}
