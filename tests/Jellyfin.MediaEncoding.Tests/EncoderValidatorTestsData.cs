namespace Jellyfin.MediaEncoding.Tests
{
    internal static class EncoderValidatorTestsData
    {
        public const string FFmpegV701Output = @"ffmpeg version 7.0.1-Jellyfin Copyright (c) 2000-2024 the FFmpeg developers
built with clang version 18.1.8
configuration: --cc=clang --pkg-config-flags=--static --extra-cflags=-I/clang64/ffbuild/include --extra-ldflags=-L/clang64/ffbuild/lib --prefix=/clang64/ffbuild/jellyfin-ffmpeg --extra-version=Jellyfin --disable-ffplay --disable-debug --disable-doc --disable-sdl2 --disable-ptx-compression --enable-lto=thin --enable-gpl --enable-version3 --enable-schannel --enable-iconv --enable-libxml2 --enable-zlib --enable-lzma --enable-gmp --enable-chromaprint --enable-libfreetype --enable-libfribidi --enable-libfontconfig --enable-libharfbuzz --enable-libass --enable-libbluray --enable-libmp3lame --enable-libopus --enable-libtheora --enable-libvorbis --enable-libopenmpt --enable-libwebp --enable-libvpx --enable-libzimg --enable-libx264 --enable-libx265 --enable-libsvtav1 --enable-libdav1d --enable-libfdk-aac --enable-opencl --enable-dxva2 --enable-d3d11va --enable-amf --enable-libvpl --enable-ffnvcodec --enable-cuda --enable-cuda-llvm --enable-cuvid --enable-nvdec --enable-nvenc
libavutil      59.  8.100 / 59.  8.100
libavcodec     61.  3.100 / 61.  3.100
libavformat    61.  1.100 / 61.  1.100
libavdevice    61.  1.100 / 61.  1.100
libavfilter    10.  1.100 / 10.  1.100
libswscale      8.  1.100 /  8.  1.100
libswresample   5.  1.100 /  5.  1.100
libpostproc    58.  1.100 / 58.  1.100";

        public const string FFmpegV611Output = @"ffmpeg version n6.1.1-16-g33efa50fa4-20240317 Copyright (c) 2000-2023 the FFmpeg developers
built with gcc 13.2.0 (crosstool-NG 1.26.0.65_ecc5e41)
configuration: --prefix=/ffbuild/prefix --pkg-config-flags=--static --pkg-config=pkg-config --cross-prefix=x86_64-w64-mingw32- --arch=x86_64 --target-os=mingw32 --enable-gpl --enable-version3 --disable-debug --enable-shared --disable-static --disable-w32threads --enable-pthreads --enable-iconv --enable-libxml2 --enable-zlib --enable-libfreetype --enable-libfribidi --enable-gmp --enable-lzma --enable-fontconfig --enable-libharfbuzz --enable-libvorbis --enable-opencl --disable-libpulse --enable-libvmaf --disable-libxcb --disable-xlib --enable-amf --enable-libaom --enable-libaribb24 --enable-avisynth --enable-chromaprint --enable-libdav1d --enable-libdavs2 --disable-libfdk-aac --enable-ffnvcodec --enable-cuda-llvm --enable-frei0r --enable-libgme --enable-libkvazaar --enable-libaribcaption --enable-libass --enable-libbluray --enable-libjxl --enable-libmp3lame --enable-libopus --enable-librist --enable-libssh --enable-libtheora --enable-libvpx --enable-libwebp --enable-lv2 --enable-libvpl --enable-openal --enable-libopencore-amrnb --enable-libopencore-amrwb --enable-libopenh264 --enable-libopenjpeg --enable-libopenmpt --enable-librav1e --enable-librubberband --enable-schannel --enable-sdl2 --enable-libsoxr --enable-libsrt --enable-libsvtav1 --enable-libtwolame --enable-libuavs3d --disable-libdrm --enable-vaapi --enable-libvidstab --enable-vulkan --enable-libshaderc --enable-libplacebo --enable-libx264 --enable-libx265 --enable-libxavs2 --enable-libxvid --enable-libzimg --enable-libzvbi --extra-cflags='$FF_CFLAGS' --extra-cxxflags='$FF_CXXFLAGS' --extra-ldflags='$FF_LDFLAGS' --extra-ldexeflags='$FF_LDEXEFLAGS' --extra-libs='$FF_LIBS' --extra-version=20240317
libavutil      58. 29.100 / 58. 29.100
libavcodec     60. 31.102 / 60. 31.102
libavformat    60. 16.100 / 60. 16.100
libavdevice    60.  3.100 / 60.  3.100
libavfilter     9. 12.100 /  9. 12.100
libswscale      7.  5.100 /  7.  5.100
libswresample   4. 12.100 /  4. 12.100
libpostproc    57.  3.100 / 57.  3.100";

        public const string FFmpegV60Output = @"ffmpeg version 6.0-Jellyfin Copyright (c) 2000-2023 the FFmpeg developers
built with gcc 12.2.0 (crosstool-NG 1.25.0.90_cf9beb1)
configuration: --prefix=/ffbuild/prefix --pkg-config=pkg-config --pkg-config-flags=--static --cross-prefix=x86_64-w64-mingw32- --arch=x86_64 --target-os=mingw32 --extra-version=Jellyfin --extra-cflags= --extra-cxxflags= --extra-ldflags= --extra-ldexeflags= --extra-libs= --enable-gpl --enable-version3 --enable-lto --disable-ffplay --disable-debug --disable-doc --disable-ptx-compression --disable-sdl2 --disable-w32threads --enable-pthreads --enable-iconv --enable-libxml2 --enable-zlib --enable-libfreetype --enable-libfribidi --enable-gmp --enable-lzma --enable-fontconfig --enable-libvorbis --enable-opencl --enable-amf --enable-chromaprint --enable-libdav1d --enable-dxva2 --enable-d3d11va --enable-libfdk-aac --enable-ffnvcodec --enable-cuda --enable-cuda-llvm --enable-cuvid --enable-nvdec --enable-nvenc --enable-libass --enable-libbluray --enable-libmp3lame --enable-libopus --enable-libtheora --enable-libvpx --enable-libwebp --enable-libvpl --enable-schannel --enable-libsrt --enable-libsvtav1 --enable-vulkan --enable-libshaderc --enable-libplacebo --enable-libx264 --enable-libx265 --enable-libzimg --enable-libzvbi
libavutil      58.  2.100 / 58.  2.100
libavcodec     60.  3.100 / 60.  3.100
libavformat    60.  3.100 / 60.  3.100
libavdevice    60.  1.100 / 60.  1.100
libavfilter     9.  3.100 /  9.  3.100
libswscale      7.  1.100 /  7.  1.100
libswresample   4. 10.100 /  4. 10.100
libpostproc    57.  1.100 / 57.  1.100";

        public const string FFmpegV512Output = @"ffmpeg version 5.1.2-Jellyfin Copyright (c) 2000-2022 the FFmpeg developers
built with gcc 10-win32 (GCC) 20220324
configuration: --prefix=/opt/ffmpeg --arch=x86_64 --target-os=mingw32 --cross-prefix=x86_64-w64-mingw32- --pkg-config=pkg-config --pkg-config-flags=--static --extra-libs='-lfftw3f -lstdc++' --extra-cflags=-DCHROMAPRINT_NODLL --extra-version=Jellyfin --disable-ffplay --disable-debug --disable-doc --disable-sdl2 --disable-ptx-compression --disable-w32threads --enable-pthreads --enable-shared --enable-lto --enable-gpl --enable-version3 --enable-schannel --enable-iconv --enable-libxml2 --enable-zlib --enable-lzma --enable-gmp --enable-chromaprint --enable-libfreetype --enable-libfribidi --enable-libfontconfig --enable-libass --enable-libbluray --enable-libmp3lame --enable-libopus --enable-libtheora --enable-libvorbis --enable-libwebp --enable-libvpx --enable-libzimg --enable-libx264 --enable-libx265 --enable-libsvtav1 --enable-libdav1d --enable-libfdk-aac --enable-opencl --enable-dxva2 --enable-d3d11va --enable-amf --enable-libmfx --enable-ffnvcodec --enable-cuda --enable-cuda-llvm --enable-cuvid --enable-nvdec --enable-nvenc
libavutil      57. 28.100 / 57. 28.100
libavcodec     59. 37.100 / 59. 37.100
libavformat    59. 27.100 / 59. 27.100
libavdevice    59.  7.100 / 59.  7.100
libavfilter     8. 44.100 /  8. 44.100
libswscale      6.  7.100 /  6.  7.100
libswresample   4.  7.100 /  4.  7.100
libpostproc    56.  6.100 / 56.  6.100";

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

        public const string FFmpegGitUnknownOutput2 = @"ffmpeg version N-g01fc3034ee-20240317 Copyright (c) 2000-2023 the FFmpeg developers
built with gcc 13.2.0 (crosstool-NG 1.26.0.65_ecc5e41)
configuration: --prefix=/ffbuild/prefix --pkg-config-flags=--static --pkg-config=pkg-config --cross-prefix=x86_64-w64-mingw32- --arch=x86_64 --target-os=mingw32 --enable-gpl --enable-version3 --disable-debug --enable-shared --disable-static --disable-w32threads --enable-pthreads --enable-iconv --enable-libxml2 --enable-zlib --enable-libfreetype --enable-libfribidi --enable-gmp --enable-lzma --enable-fontconfig --enable-libvorbis --enable-opencl --disable-libpulse --disable-libxcb --disable-xlib --enable-amf --enable-libaom --enable-libaribb24 --enable-avisynth --disable-chromaprint --enable-libdav1d --enable-libdavs2 --disable-libfdk-aac --enable-ffnvcodec --enable-cuda-llvm --disable-frei0r --enable-libgme --enable-libkvazaar --enable-libass --enable-libbluray --enable-libmp3lame --enable-libopus --enable-librist --enable-libssh --enable-libtheora --enable-libvpx --enable-libwebp --enable-lv2 --disable-openal --enable-libopencore-amrnb --enable-libopencore-amrwb --enable-libopenh264 --enable-libopenjpeg --enable-libopenmpt --enable-librav1e --enable-librubberband --enable-schannel --enable-sdl2 --enable-libsoxr --enable-libsrt --enable-libsvtav1 --enable-libtwolame --enable-libuavs3d --disable-libdrm --disable-vaapi --enable-libvidstab --disable-vulkan --enable-libx264 --enable-libx265 --enable-libxavs2 --enable-libxvid --enable-libzimg --enable-libzvbi --extra-cflags='$FF_CFLAGS' --extra-cxxflags='$FF_CXXFLAGS' --extra-ldflags='$FF_LDFLAGS' --extra-ldexeflags='$FF_LDEXEFLAGS' --extra-libs='$FF_LIBS' --extra-version=20240317
libavutil      56. 70.100 / 56. 70.100
libavcodec     58.134.100 / 58.134.100
libavformat    58. 76.100 / 58. 76.100
libavdevice    58. 13.100 / 58. 13.100
libavfilter     7.110.100 /  7.110.100
libswscale      5.  9.100 /  5.  9.100
libswresample   3.  9.100 /  3.  9.100
libpostproc    55.  9.100 / 55.  9.100";

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
