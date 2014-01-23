#!/bin/bash -e

CWD=$(pwd)
MACHINEARCH=$(uname -m)
BUILDPATH="mediabrowser"
MKGITPATH="mkbundlex"
LOGPATH="$CWD/$BUILDPATH/logs_mkbundlex"
LOGDATE=$(date +%s)

if [ $# -ne 1 ]; then
    echo "Require an archive to be converted."
else
    if [ ! -e "$1" ]; then
        echo "File doesn't exist."
    else
        if [[ ! $1 == *.tar.gz ]]; then
            echo "Not a tar.gz file"
        else
            FILENAME=${1##*/}
            RELEASENAME=${FILENAME%%.tar.gz}
            TEMP1=${FILENAME##*bundlex.}
            VERSION=${TEMP1%%.tar.gz}
            if [$1 == /*]; then
                ARCHIVEPATH=$1
            else
                ARCHIVEPATH=$CWD/$1
            fi
        fi
    fi
fi

if [ $MACHINEARCH == "x86_64" ]; then
    RELEASEPATH="MediaBrowser.Mono.mkbundlex.x86_64.$VERSION"
else
    RELEASEPATH="MediaBrowser.Mono.mkbundlex.i686.$VERSION"
fi

echo "========================================"
echo "         MediaBrowser for Mono"
echo "            Mkbundle Creator"
echo ""
echo "   Logs available in $BUILDPATH/logs_mkbundlex"
echo "    Archive available in $BUILDPATH"
echo "========================================"
echo ""
echo "========================================"
echo "          Check Dependencies"
echo "========================================"
echo ""

# http://stackoverflow.com/questions/4023830/bash-how-compare-two-strings-in-version-format
vercomp () {
    if [[ $1 == $2 ]]
    then
        return 0
    fi
    local IFS=.
    local i ver1=($1) ver2=($2)
    # fill empty fields in ver1 with zeros
    for ((i=${#ver1[@]}; i<${#ver2[@]}; i++))
    do
        ver1[i]=0
    done
    for ((i=0; i<${#ver1[@]}; i++))
    do
        if [[ -z ${ver2[i]} ]]
        then
            # fill empty fields in ver2 with zeros
            ver2[i]=0
        fi
        if ((10#${ver1[i]} > 10#${ver2[i]}))
        then
            return 1 # Greater than
        fi
        if ((10#${ver1[i]} < 10#${ver2[i]}))
        then
            return 2 # Less than
        fi
    done
    return 0 # Equal
}

echo "Checking for git"
if ! type -P git &>/dev/null; then
    echo "Git not found. Please install."
    exit
fi

echo "Checking for tar"
if ! type -P tar &>/dev/null; then
    echo "Tar not found. Please install."
    exit
fi

echo "Checking for gzip"
if ! type -P gzip &>/dev/null; then
    echo "Gzip not found. Please install."
    exit
fi

echo "Checking for mono"
if ! type -P mono &>/dev/null; then
    echo "Mono not found. Please install."
    exit
else
    MONOVERSION=$(mono --version | awk '$1 == "Mono" {print $5 }')
    vercomp $MONOVERSION "3.2.7"
    if [ $? -eq 2 ]; then
        echo "Require Mono version 3.2.7 and higher."
        exit
    fi
    TEMP2=$(type -P mono)
    MONOPATH=${TEMP2%%/bin/mono}
    if [ -e "$2" ] && [ "$2" == "lib64" ]; then
        MONOLIBPATH="$MONOPATH/lib64"
    else
        MONOLIBPATH="$MONOPATH/lib"
    fi
fi

echo ""
echo "========================================"
echo "           Requirements"
echo "========================================"
echo ""

echo "Check if folder $BUILDPATH exist"
if [ ! -d "$BUILDPATH" ]; then
    echo "Creating folder: $BUILDPATH"
    mkdir $BUILDPATH
    cd $BUILDPATH
else
    echo "$BUILDPATH exist, checking for old binaries folder"
    cd $BUILDPATH
    for i in $(find -maxdepth 1 -type d -name MediaBrowser.Mono.\* -printf "%f\n");
    do
        echo "Removing old binaries folder: $CWD/$BUILDPATH/$i"
        rm -rf "$CWD/$BUILDPATH/$i"
    done
fi

echo "Check if folder $BUILDPATH/logs exist"
if [ ! -d "$LOGPATH" ]; then
    echo "Creating folder: $BUILDPATH/logs"
    mkdir $LOGPATH
fi

echo ""
echo "========================================"
echo "       Retrieving mkbundlex from git"
echo "========================================"
echo ""

if [ ! -d "$MKGITPATH" ]; then
    mkdir $MKGITPATH
    echo "Git cloning into $MKGITPATH"
    git clone https://github.com/RaveNoX/mkbundlex.git $MKGITPATH > "$LOGPATH/gitclone_stdout_$LOGDATE.log" 2> "$LOGPATH/gitclone_stderr_$LOGDATE.log"
    cd $MKGITPATH
else
    cd $MKGITPATH
    echo "Folder $MKGITPATH already present, checking if it's a git repo and the correct one."
    if ! git rev-parse; then
        echo "Not a git repo."
        exit
    fi
    MBFETCHURL=$(git remote -v | awk '$1 == "origin" && $3 == "(fetch)"  {print $2 }')
    if [ "$MBFETCHURL" != "https://github.com/RaveNoX/mkbundlex.git" ]; then
        echo "Wrong git repo."
        exit
    fi
    echo "Git pull and checkout master"
    git pull > "$LOGPATH/gitpull_stdout_$LOGDATE.log" 2> "$LOGPATH/gitpull_stderr_$LOGDATE.log"
    git checkout master > "$LOGPATH/gitco_stdout_$LOGDATE.log" 2> "$LOGPATH/gitco_stderr_$LOGDATE.log"
fi

if [ ! -e "$CWD/$BUILDPATH/mkbundlex.exe" ]; then
    echo ""
    echo "========================================"
    echo "          Building mkbundlex"
    echo "========================================"
    echo ""

    cd mkbundlex

    echo "xbuild: cleaning build folder"
    xbuild /p:Configuration=Release /t:clean mkbundlex.sln > "$LOGPATH/xbuildclean_stdout_$LOGDATE.log" 2> "$LOGPATH/xbuildclean_stderr_$LOGDATE.log"

    echo "xbuild: building..."
    xbuild /p:Configuration=Release /t:build mkbundlex.sln > "$LOGPATH/xbuild_stdout_$LOGDATE.log" 2> "$LOGPATH/xbuild_stderr_$LOGDATE.log"
    echo "xbuild: building done"

    echo "Copying mkbundlex.exe to $BUILDPATH"
    cp -f "src/bin/Release/mkbundlex.exe" "$CWD/$BUILDPATH/"
fi

cd "$CWD/$BUILDPATH/"

echo ""
echo "========================================"
echo "       Create release folder"
echo "========================================"
echo ""

echo "Create release folder: $RELEASEPATH"
mkdir "$RELEASEPATH"

echo "Extracting file $FILENAME in $RELEASENAME"
tar zxf $ARCHIVEPATH

echo "Copying dashboard-ui folder from archive"
cp -rf "$CWD/$BUILDPATH/$RELEASENAME/dashboard-ui" "$CWD/$BUILDPATH/$RELEASEPATH/"

echo "Copying sqlite3 folder from archive"
cp -rf "$CWD/$BUILDPATH/$RELEASENAME/sqlite3" "$CWD/$BUILDPATH/$RELEASEPATH/"

echo "Copying swagger-ui folder from archive"
cp -rf "$CWD/$BUILDPATH/$RELEASENAME/swagger-ui" "$CWD/$BUILDPATH/$RELEASEPATH/"

echo "Copying libMonoPosixHelper.so and etc/mono"
if [ $MACHINEARCH == "x86_64" ]; then
    mkdir "$RELEASEPATH/lib64"
    mkdir -p "$RELEASEPATH/mono_config.x86_64/mono"
    cp -f "$MONOLIBPATH/libMonoPosixHelper.so" "$RELEASEPATH/lib64/"
    cp -rf "$MONOPATH/etc/mono" "$RELEASEPATH/mono_config.x86_64/"
else
    mkdir "$RELEASEPATH/lib"
    mkdir -p "$RELEASEPATH/mono_config.i686/mono"
    cp -f "$MONOLIBPATH/libMonoPosixHelper.so" "$RELEASEPATH/lib/"
    cp -rf "$MONOPATH/etc/mono" "$RELEASEPATH/mono_config.i686/"
fi

echo "Generating Mono config file"
if [ $MACHINEARCH == "x86_64" ]; then
    cat > "$RELEASEPATH/mono_config.x86_64/mono/config" << EOL
<configuration>
        <dllmap dll="i:cygwin1.dll" target="libc.so.6" os="!windows" />
        <dllmap dll="libc" target="libc.so.6" os="!windows"/>
        <dllmap dll="intl" target="libc.so.6" os="!windows"/>
        <dllmap dll="intl" name="bind_textdomain_codeset" target="libc.so.6" os="solaris"/>
        <dllmap dll="libintl" name="bind_textdomain_codeset" target="libc.so.6" os="solaris"/>
        <dllmap dll="libintl" target="libc.so.6" os="!windows"/>
        <dllmap dll="i:libxslt.dll" target="libxslt.so" os="!windows"/>
        <dllmap dll="i:odbc32.dll" target="libodbc.so" os="!windows"/>
        <dllmap dll="i:odbc32.dll" target="libiodbc.dylib" os="osx"/>
        <dllmap dll="oci" target="libclntsh.so" os="!windows"/>
        <dllmap dll="db2cli" target="libdb2_36.so" os="!windows"/>
        <dllmap dll="MonoPosixHelper" target="./lib64/libMonoPosixHelper.so" os="!windows" />
        <dllmap dll="i:msvcrt" target="libc.so.6" os="!windows"/>
        <dllmap dll="i:msvcrt.dll" target="libc.so.6" os="!windows"/>
        <dllmap dll="sqlite" target="libsqlite.so.0" os="!windows"/>
        <dllmap dll="sqlite3" target="./sqlite3/linux/lib64/libsqlite3.so.0.8.6" os="!windows"/>
        <dllmap dll="libX11" target="libX11.so.6" os="!windows" />
        <dllmap dll="libcairo-2.dll" target="libcairo.so.2" os="!windows"/>
        <dllmap dll="libcairo-2.dll" target="libcairo.2.dylib" os="osx"/>
        <dllmap dll="libcups" target="libcups.so.2" os="!windows"/>
        <dllmap dll="libcups" target="libcups.dylib" os="osx"/>
        <dllmap dll="i:kernel32.dll">
                <dllentry dll="__Internal" name="CopyMemory" target="mono_win32_compat_CopyMemory"/>
                <dllentry dll="__Internal" name="FillMemory" target="mono_win32_compat_FillMemory"/>
                <dllentry dll="__Internal" name="MoveMemory" target="mono_win32_compat_MoveMemory"/>
                <dllentry dll="__Internal" name="ZeroMemory" target="mono_win32_compat_ZeroMemory"/>
        </dllmap>
        <dllmap dll="gdiplus" target="/usr/lib64/libgdiplus.so" os="!windows"/>
        <dllmap dll="gdiplus.dll" target="/usr/lib64/libgdiplus.so"  os="!windows"/>
</configuration>
EOL
else
    cat > "$RELEASEPATH/mono_config.i686/mono/config" << EOL
<configuration>
        <dllmap dll="i:cygwin1.dll" target="libc.so.6" os="!windows" />
        <dllmap dll="libc" target="libc.so.6" os="!windows"/>
        <dllmap dll="intl" target="libc.so.6" os="!windows"/>
        <dllmap dll="intl" name="bind_textdomain_codeset" target="libc.so.6" os="solaris"/>
        <dllmap dll="libintl" name="bind_textdomain_codeset" target="libc.so.6" os="solaris"/>
        <dllmap dll="libintl" target="libc.so.6" os="!windows"/>
        <dllmap dll="i:libxslt.dll" target="libxslt.so" os="!windows"/>
        <dllmap dll="i:odbc32.dll" target="libodbc.so" os="!windows"/>
        <dllmap dll="i:odbc32.dll" target="libiodbc.dylib" os="osx"/>
        <dllmap dll="oci" target="libclntsh.so" os="!windows"/>
        <dllmap dll="db2cli" target="libdb2_36.so" os="!windows"/>
        <dllmap dll="MonoPosixHelper" target="./lib/libMonoPosixHelper.so" os="!windows" />
        <dllmap dll="i:msvcrt" target="libc.so.6" os="!windows"/>
        <dllmap dll="i:msvcrt.dll" target="libc.so.6" os="!windows"/>
        <dllmap dll="sqlite" target="libsqlite.so.0" os="!windows"/>
        <dllmap dll="sqlite3" target="./sqlite3/linux/lib/libsqlite3.so.0.8.6" os="!windows"/>
        <dllmap dll="libX11" target="libX11.so.6" os="!windows" />
        <dllmap dll="libcairo-2.dll" target="libcairo.so.2" os="!windows"/>
        <dllmap dll="libcairo-2.dll" target="libcairo.2.dylib" os="osx"/>
        <dllmap dll="libcups" target="libcups.so.2" os="!windows"/>
        <dllmap dll="libcups" target="libcups.dylib" os="osx"/>
        <dllmap dll="i:kernel32.dll">
                <dllentry dll="__Internal" name="CopyMemory" target="mono_win32_compat_CopyMemory"/>
                <dllentry dll="__Internal" name="FillMemory" target="mono_win32_compat_FillMemory"/>
                <dllentry dll="__Internal" name="MoveMemory" target="mono_win32_compat_MoveMemory"/>
                <dllentry dll="__Internal" name="ZeroMemory" target="mono_win32_compat_ZeroMemory"/>
        </dllmap>
        <dllmap dll="gdiplus" target="/usr/lib/libgdiplus.so" os="!windows"/>
        <dllmap dll="gdiplus.dll" target="/usr/lib/libgdiplus.so"  os="!windows"/>
</configuration>
EOL
fi

echo ""
echo "========================================"
echo "      Creating mkbundlex release"
echo "========================================"
echo ""

cd "$RELEASENAME"

if [ $MACHINEARCH == "x86_64" ]; then
    mono ../mkbundlex.exe --static --deps -o "../$RELEASEPATH/MediaBrowser.Server.Mono.x86_64" -z --config-dir ./mono_config.x86_64/ MediaBrowser.Server.Mono.exe $MONOLIBPATH/mono/gac/I18N/4.0.0.0__0738eb9f132ed756/I18N.dll $MONOLIBPATH/mono/gac/I18N.West/4.0.0.0__0738eb9f132ed756/I18N.West.dll $MONOLIBPATH/mono/gac/I18N.CJK/4.0.0.0__0738eb9f132ed756/I18N.CJK.dll $MONOLIBPATH/mono/gac/I18N.MidEast/4.0.0.0__0738eb9f132ed756/I18N.MidEast.dll $MONOLIBPATH/mono/gac/I18N.Other/4.0.0.0__0738eb9f132ed756/I18N.Other.dll $MONOLIBPATH/mono/gac/I18N.Rare/4.0.0.0__0738eb9f132ed756/I18N.Rare.dll > "$LOGPATH/mkbundlex_stdout_$LOGDATE.log" 2> "$LOGPATH/mkbundlex_stderr_$LOGDATE.log"
else
    mono ../mkbundlex.exe --static --deps -o "../$RELEASEPATH/MediaBrowser.Server.Mono.i686" -z --config-dir ./mono_config.i686/ MediaBrowser.Server.Mono.exe $MONOLIBPATH/mono/gac/I18N/4.0.0.0__0738eb9f132ed756/I18N.dll $MONOLIBPATH/mono/gac/I18N.West/4.0.0.0__0738eb9f132ed756/I18N.West.dll $MONOLIBPATH/mono/gac/I18N.CJK/4.0.0.0__0738eb9f132ed756/I18N.CJK.dll $MONOLIBPATH/mono/gac/I18N.MidEast/4.0.0.0__0738eb9f132ed756/I18N.MidEast.dll $MONOLIBPATH/mono/gac/I18N.Other/4.0.0.0__0738eb9f132ed756/I18N.Other.dll $MONOLIBPATH/mono/gac/I18N.Rare/4.0.0.0__0738eb9f132ed756/I18N.Rare.dll > "$LOGPATH/mkbundlex_stdout_$LOGDATE.log" 2> "$LOGPATH/mkbundlex_stderr_$LOGDATE.log"
fi

cd ..

echo ""
echo "========================================"
echo "       Creating tar.gz archive"
echo "========================================"
echo ""

cd "$CWD/$BUILDPATH"

echo "Creating $RELEASEPATH.tar.gz"
if [ -e "$CWD/$BUILDPATH/$RELEASEPATH.tar.gz" ]; then
    echo "Destination file exist: $CWD/$BUILDPATH/$RELEASEPATH.tar.gz"
    exit
fi

tar -czf "$RELEASEPATH.tar.gz" "$RELEASEPATH" > "$LOGPATH/tar_stdout_$LOGDATE.log" 2> "$LOGPATH/tar_stderr_$LOGDATE.log"

rm -rf "$CWD/$BUILDPATH/MediaBrowser.Mono.mkbundlex.$VERSION"

echo ""
echo "========================================"
echo "               Cleanup"
echo "========================================"

echo "Removing $RELEASEPATH folder"
rm -rf "$CWD/$BUILDPATH/$RELEASEPATH"
echo "Removing $RELEASENAME folder"
rm -rf "$CWD/$BUILDPATH/$RELEASENAME"

cd "$CWD"

echo ""
echo "========================================"
echo "                 Done"
echo "========================================"

