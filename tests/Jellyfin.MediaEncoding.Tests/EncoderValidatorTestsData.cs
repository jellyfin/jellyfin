namespace Jellyfin.MediaEncoding.Tests
{
    internal static class EncoderValidatorTestsData
    {
        public const string FFmpegV44Output = @"ffmpeg version 4.4-Jellyfin Copyright (c) 2000-2021 the FFmpeg developers
built with gcc 10.3.0 (Rev5, Built by MSYS2 project)
configuration:  --disable-static --enable-shared --extra-version=Jellyfin --disable-ffplay --disable-debug --enable-gpl --enable-version3 --enable-bzlib --enable-iconv --enable-lzma --enable-zlib --enable-sdl2 --enable-fontconfig --enable-gmp --enable-libass --enable-libzimg --enable-libbluray --enable-libfreetype --enable-libmp3lame --enable-libopus --enable-libtheora --enable-libvorbis --enable-libwebp --enable-libvpx --enable-libx264 --enable-libx265 --enable-libdav1d --enable-opencl --enable-dxva2 --enable-d3d11va --enable-amf --enable-libmfx --enable-cuda --enable-cuda-llvm --enable-cuvid --enable-nvenc --enable-nvdec --enable-ffnvcodec --enable-gnutls
libavutil      56. 70.100 / 56. 70.100
libavcodec     58.134.100 / 58.134.100
libavformat    58. 76.100 / 58. 76.100
libavdevice    58. 13.100 / 58. 13.100
libavfilter     7.110.100 /  7.110.100
libswscale      5.  9.100 /  5.  9.100
libswresample   3.  9.100 /  3.  9.100
libpostproc    55.  9.100 / 55.  9.100";

        public const string FFmpegV432Output = @"ffmpeg version n4.3.2-Jellyfin Copyright (c) 2000-2021 the FFmpeg developers
built with gcc 10.2.0 (Rev9, Built by MSYS2 project)
configuration:  --disable-static --enable-shared --cc='ccache gcc' --cxx='ccache g++' --extra-version=Jellyfin --disable-ffplay --disable-debug --enable-lto --enable-gpl --enable-version3 --enable-bzlib --enable-iconv --enable-lzma --enable-zlib --enable-sdl2 --enable-fontconfig --enable-gmp --enable-libass --enable-libzimg --enable-libbluray --enable-libfreetype --enable-libmp3lame --enable-libopus --enable-libtheora --enable-libvorbis --enable-libwebp --enable-libvpx --enable-libx264 --enable-libx265 --enable-libdav1d --enable-opencl --enable-dxva2 --enable-d3d11va --enable-amf --enable-libmfx --enable-cuda --enable-cuda-llvm --enable-cuvid --enable-nvenc --enable-nvdec --enable-ffnvcodec --enable-gnutls
libavutil      56. 51.100 / 56. 51.100
libavcodec     58. 91.100 / 58. 91.100
libavformat    58. 45.100 / 58. 45.100
libavdevice    58. 10.100 / 58. 10.100
libavfilter     7. 85.100 /  7. 85.100
libswscale      5.  7.100 /  5.  7.100
libswresample   3.  7.100 /  3.  7.100
libpostproc    55.  7.100 / 55.  7.100";

        public const string FFmpegV431Output = @"ffmpeg version n4.3.1 Copyright (c) 2000-2020 the FFmpeg developers
built with gcc 10.1.0 (GCC)
configuration: --prefix=/usr --disable-debug --disable-static --disable-stripping --enable-avisynth --enable-fontconfig --enable-gmp --enable-gnutls --enable-gpl --enable-ladspa --enable-libaom --enable-libass --enable-libbluray --enable-libdav1d --enable-libdrm --enable-libfreetype --enable-libfribidi --enable-libgsm --enable-libiec61883 --enable-libjack --enable-libmfx --enable-libmodplug --enable-libmp3lame --enable-libopencore_amrnb --enable-libopencore_amrwb --enable-libopenjpeg --enable-libopus --enable-libpulse --enable-librav1e --enable-libsoxr --enable-libspeex --enable-libsrt --enable-libssh --enable-libtheora --enable-libv4l2 --enable-libvidstab --enable-libvmaf --enable-libvorbis --enable-libvpx --enable-libwebp --enable-libx264 --enable-libx265 --enable-libxcb --enable-libxml2 --enable-libxvid --enable-nvdec --enable-nvenc --enable-omx --enable-shared --enable-version3
libavutil      56. 51.100 / 56. 51.100
libavcodec     58. 91.100 / 58. 91.100
libavformat    58. 45.100 / 58. 45.100
libavdevice    58. 10.100 / 58. 10.100
libavfilter     7. 85.100 /  7. 85.100
libswscale      5.  7.100 /  5.  7.100
libswresample   3.  7.100 /  3.  7.100
libpostproc    55.  7.100 / 55.  7.100";

        public const string FFmpegV43Output = @"ffmpeg version 4.3 Copyright (c) 2000-2020 the FFmpeg developers
built with gcc 7 (Ubuntu 7.5.0-3ubuntu1~18.04)
configuration: --prefix=/usr/lib/jellyfin-ffmpeg --target-os=linux --disable-doc --disable-ffplay --disable-shared --disable-libxcb --disable-vdpau --disable-sdl2 --disable-xlib --enable-gpl --enable-version3 --enable-static --enable-libfontconfig --enable-fontconfig --enable-gmp --enable-gnutls --enable-libass --enable-libbluray --enable-libdrm --enable-libfreetype --enable-libfribidi --enable-libmp3lame --enable-libopus --enable-libtheora --enable-libvorbis --enable-libwebp --enable-libx264 --enable-libx265 --enable-libzvbi --arch=amd64 --enable-amf --enable-nvenc --enable-nvdec --enable-vaapi --enable-opencl
libavutil      56. 51.100 / 56. 51.100
libavcodec     58. 91.100 / 58. 91.100
libavformat    58. 45.100 / 58. 45.100
libavdevice    58. 10.100 / 58. 10.100
libavfilter     7. 85.100 /  7. 85.100
libswscale      5.  7.100 /  5.  7.100
libswresample   3.  7.100 /  3.  7.100
libpostproc    55.  7.100 / 55.  7.100";

        public const string FFmpegV421Output = @"ffmpeg version 4.2.1 Copyright (c) 2000-2019 the FFmpeg developers
built with gcc 9.1.1 (GCC) 20190807
configuration: --enable-gpl --enable-version3 --enable-sdl2 --enable-fontconfig --enable-gnutls --enable-iconv --enable-libass --enable-libdav1d --enable-libbluray --enable-libfreetype --enable-libmp3lame --enable-libopencore-amrnb --enable-libopencore-amrwb --enable-libopenjpeg --enable-libopus --enable-libshine --enable-libsnappy --enable-libsoxr --enable-libtheora --enable-libtwolame --enable-libvpx --enable-libwavpack --enable-libwebp --enable-libx264 --enable-libx265 --enable-libxml2 --enable-libzimg --enable-lzma --enable-zlib --enable-gmp --enable-libvidstab --enable-libvorbis --enable-libvo-amrwbenc --enable-libmysofa --enable-libspeex --enable-libxvid --enable-libaom --enable-libmfx --enable-amf --enable-ffnvcodec --enable-cuvid --enable-d3d11va --enable-nvenc --enable-nvdec --enable-dxva2 --enable-avisynth --enable-libopenmpt
libavutil      56. 31.100 / 56. 31.100
libavcodec     58. 54.100 / 58. 54.100
libavformat    58. 29.100 / 58. 29.100
libavdevice    58.  8.100 / 58.  8.100
libavfilter     7. 57.100 /  7. 57.100
libswscale      5.  5.100 /  5.  5.100
libswresample   3.  5.100 /  3.  5.100
libpostproc    55.  5.100 / 55.  5.100";

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
libpostproc    55.  5.100 / 55.  5.100";

        public const string FFmpegV414Output = @"ffmpeg version 4.1.4-1~deb10u1 Copyright (c) 2000-2019 the FFmpeg developers
built with gcc 8 (Raspbian 8.3.0-6+rpi1)
configuration: --prefix=/usr --extra-version='1~deb10u1' --toolchain=hardened --libdir=/usr/lib/arm-linux-gnueabihf --incdir=/usr/include/arm-linux-gnueabihf --arch=arm --enable-gpl --disable-stripping --enable-avresample --disable-filter=resample --enable-avisynth --enable-gnutls --enable-ladspa --enable-libaom --enable-libass --enable-libbluray --enable-libbs2b --enable-libcaca --enable-libcdio --enable-libcodec2 --enable-libflite --enable-libfontconfig --enable-libfreetype --enable-libfribidi --enable-libgme --enable-libgsm --enable-libjack --enable-libmp3lame --enable-libmysofa --enable-libopenjpeg --enable-libopenmpt --enable-libopus --enable-libpulse --enable-librsvg --enable-librubberband --enable-libshine --enable-libsnappy --enable-libsoxr --enable-libspeex --enable-libssh --enable-libtheora --enable-libtwolame --enable-libvidstab --enable-libvorbis --enable-libvpx --enable-libwavpack --enable-libwebp --enable-libx265 --enable-libxml2 --enable-libxvid --enable-libzmq --enable-libzvbi --enable-lv2 --enable-omx --enable-openal --enable-opengl --enable-sdl2 --enable-libdc1394 --enable-libdrm --enable-libiec61883 --enable-chromaprint --enable-frei0r --enable-libx264 --enable-shared
libavutil      56. 22.100 / 56. 22.100
libavcodec     58. 35.100 / 58. 35.100
libavformat    58. 20.100 / 58. 20.100
libavdevice    58.  5.100 / 58.  5.100
libavfilter     7. 40.101 /  7. 40.101
libavresample   4.  0.  0 /  4.  0.  0
libswscale      5.  3.100 /  5.  3.100
libswresample   3.  3.100 /  3.  3.100
libpostproc    55.  3.100 / 55.  3.100";

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
libpostproc    55.  1.100 / 55.  1.100";

        public const string FFmpegGitUnknownOutput2 = @"ffmpeg version N-94303-g7cb4f8c962 Copyright (c) 2000-2019 the FFmpeg developers
built with gcc 9.1.1 (GCC) 20190716
configuration: --enable-gpl --enable-version3 --enable-sdl2 --enable-fontconfig --enable-gnutls --enable-iconv --enable-libass --enable-libdav1d --enable-libbluray --enable-libfreetype --enable-libmp3lame --enable-libopencore-amrnb --enable-libopencore-amrwb --enable-libopenjpeg --enable-libopus --enable-libshine --enable-libsnappy --enable-libsoxr --enable-libtheora --enable-libtwolame --enable-libvpx --enable-libwavpack --enable-libwebp --enable-libx264 --enable-libx265 --enable-libxml2 --enable-libzimg --enable-lzma --enable-zlib --enable-gmp --enable-libvidstab --enable-libvorbis --enable-libvo-amrwbenc --enable-libmysofa --enable-libspeex --enable-libxvid --enable-libaom --enable-libmfx --enable-amf --enable-ffnvcodec --enable-cuvid --enable-d3d11va --enable-nvenc --enable-nvdec --enable-dxva2 --enable-avisynth --enable-libopenmpt
libavutil      56. 30.100 / 56. 30.100
libavcodec     58. 53.101 / 58. 53.101
libavformat    58. 28.102 / 58. 28.102
libavdevice    58.  7.100 / 58.  7.100
libavfilter     7. 56.101 /  7. 56.101
libswscale      5.  4.101 /  5.  4.101
libswresample   3.  4.100 /  3.  4.100
libpostproc    55.  4.100 / 55.  4.100";

        public const string FFmpegGitUnknownOutput = @"ffmpeg version N-45325-gb173e0353-static https://johnvansickle.com/ffmpeg/  Copyright (c) 2000-2018 the FFmpeg developers
built with gcc 6.3.0 (Debian 6.3.0-18+deb9u1) 20170516
configuration: --enable-gpl --enable-version3 --enable-static --disable-debug --disable-ffplay --disable-indev=sndio --disable-outdev=sndio --cc=gcc-6 --enable-fontconfig --enable-frei0r --enable-gnutls --enable-gray --enable-libfribidi --enable-libass --enable-libfreetype --enable-libmp3lame --enable-libopencore-amrnb --enable-libopencore-amrwb --enable-libopenjpeg --enable-librubberband --enable-libsoxr --enable-libspeex --enable-libvorbis --enable-libopus --enable-libtheora --enable-libvidstab --enable-libvo-amrwbenc --enable-libvpx --enable-libwebp --enable-libx264 --enable-libx265 --enable-libxvid --enable-libzimg
libavutil      56.  9.100 / 56.  9.100
libavcodec     58. 14.100 / 58. 14.100
libavformat    58. 10.100 / 58. 10.100
libavdevice    58.  2.100 / 58.  2.100
libavfilter     7. 13.100 /  7. 13.100
libswscale      5.  0.102 /  5.  0.102
libswresample   3.  0.101 /  3.  0.101
libpostproc    55.  0.100 / 55.  0.100";
    }
}
