#!/bin/bash -e

CWD=$(pwd)
BUILDPATH="mediabrowser"
MBGITPATH="MediaBrowser"
LOGPATH="$CWD/$BUILDPATH/logs"
LOGDATE=$(date +%s)

echo "========================================"
echo "         MediaBrowser for Mono"
echo "            Package Creator"
echo ""
echo "   Logs available in $BUILDPATH/logs"
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
        echo 0
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
            echo 1 # Greater than
        fi
        if ((10#${ver1[i]} < 10#${ver2[i]}))
        then
            echo 2 # Less than
        fi
    done
    echo 0 # Equal
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
    compResult=$(vercomp $MONOVERSION "3.2.7")
    if [ $compResult -eq 2 ]; then
        echo "Require Mono version 3.2.7 and higher."
        exit
    fi
fi

echo "Checking for mozroots"
if ! type -P mozroots &>/dev/null; then
    echo "Mozroots not found. Please install."
    exit
fi

echo "Checking for xbuild"
if ! type -P xbuild &>/dev/null; then
    echo "Xbuild not found. Please install."
    exit
fi

echo "Checking for monodis"
if ! type -P monodis &>/dev/null; then
    echo "Monodis not found. Please install. (mono-utils package on ubuntu!)"
    exit
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
echo "       Retrieving source from git"
echo "========================================"
echo ""

if [ ! -d "$MBGITPATH" ]; then
    mkdir $MBGITPATH
    echo "Git cloning into $MBGITPATH"
    git clone https://github.com/MediaBrowser/MediaBrowser.git $MBGITPATH > "$LOGPATH/gitclone_stdout_$LOGDATE.log" 2> "$LOGPATH/gitclone_stderr_$LOGDATE.log"
    cd $MBGITPATH
else
    cd $MBGITPATH
    echo "Folder $MBGITPATH already present, checking if it's a git repo and the correct one."
    if ! git rev-parse; then
        echo "Not a git repo."
        exit
    fi
    MBFETCHURL=$(git remote -v | awk '$1 == "origin" && $3 == "(fetch)"  {print $2 }')
    if [ "$MBFETCHURL" != "https://github.com/MediaBrowser/MediaBrowser.git" ]; then
        echo "Wrong git repo."
        exit
    fi
    echo "Git pull and checkout master"
    git pull > "$LOGPATH/gitpull_stdout_$LOGDATE.log" 2> "$LOGPATH/gitpull_stderr_$LOGDATE.log"
    git checkout master > "$LOGPATH/gitco_stdout_$LOGDATE.log" 2> "$LOGPATH/gitco_stderr_$LOGDATE.log"
fi

echo ""
echo "========================================"
echo "       Nuget: Restoring packages"
echo "========================================"
echo ""

echo "Importing trusted root certificates from Mozilla LXR"
mozroots --import --sync > "$LOGPATH/mozroots_stdout_$LOGDATE.log" 2> "$LOGPATH/mozroots_stderr_$LOGDATE.log"
echo "Updating NuGet to the latest version"
mono .nuget/NuGet.exe update -self > "$LOGPATH/nugetupdate_stdout_$LOGDATE.log" 2> "$LOGPATH/nugetupdate_stderr_$LOGDATE.log"
# echo "Restoring NuGet package"
# mono .nuget/NuGet.exe restore MediaBrowser.Mono.sln > "$LOGPATH/nugetrestore_stdout_$LOGDATE.log" 2> "$LOGPATH/nugetrestore_stderr_$LOGDATE.log"


echo ""
echo "========================================"
echo "         Building MediaBrowser"
echo "========================================"
echo ""

echo "xbuild: cleaning build folder"
xbuild /p:Configuration="Release Mono" /p:Platform="Any CPU" /t:clean MediaBrowser.Mono.sln > "$LOGPATH/xbuildclean_stdout_$LOGDATE.log" 2> "$LOGPATH/xbuildclean_stderr_$LOGDATE.log"

echo "xbuild: building..."
xbuild /p:Configuration="Release Mono" /p:Platform="Any CPU" /t:build MediaBrowser.Mono.sln > "$LOGPATH/xbuild_stdout_$LOGDATE.log" 2> "$LOGPATH/xbuild_stderr_$LOGDATE.log"
echo "xbuild: building done"

echo ""
echo "========================================"
echo "       Creating tar.gz archive"
echo "========================================"
echo ""

cd "MediaBrowser.Server.Mono/bin/Release Mono"

echo "Retreiving MediaBrowser version"
MBVERSION=$(monodis --assembly MediaBrowser.Server.Mono.exe | awk '$1 == "Version:" {print $2 }')

if [ -z "$MBVERSION" ]; then
    echo "Unable to get Mediabrowser version from monodis."
    exit
fi

echo "Creating MediaBrowser.Mono.$MBVERSION.tar.gz"
if [ -e "$CWD/$BUILDPATH/MediaBrowser.Mono.$MBVERSION.tar.gz" ]; then
    echo "Destination file exist: $CWD/$BUILDPATH/MediaBrowser.Mono.$MBVERSION.tar.gz"
    exit
fi
mkdir "$CWD/$BUILDPATH/MediaBrowser.Mono.$MBVERSION"
cp -fR * "$CWD/$BUILDPATH/MediaBrowser.Mono.$MBVERSION/"
cd "$CWD/$BUILDPATH"

tar -czf "MediaBrowser.Mono.$MBVERSION.tar.gz" "MediaBrowser.Mono.$MBVERSION" > "$LOGPATH/tar_stdout_$LOGDATE.log" 2> "$LOGPATH/tar_stderr_$LOGDATE.log"

rm -rf "$CWD/$BUILDPATH/MediaBrowser.Mono.$MBVERSION"

echo ""
echo "========================================"
echo "     Building MediaBrowser mkbundlex"
echo "========================================"
echo ""

cd "$CWD/$BUILDPATH/$MBGITPATH"

echo "xbuild: cleaning build folder"
xbuild /p:Configuration="Release Mono" /p:Platform="Any CPU" /p:DefineConstants="MONOMKBUNDLE" /t:clean MediaBrowser.Mono.sln > "$LOGPATH/xbuildmkclean_stdout_$LOGDATE.log" 2> "$LOGPATH/xbuildmkclean_stderr_$LOGDATE.log"

echo "xbuild: building..."
xbuild /p:Configuration="Release Mono" /p:Platform="Any CPU" /p:DefineConstants="MONOMKBUNDLE" /t:build MediaBrowser.Mono.sln > "$LOGPATH/xbuildmk_stdout_$LOGDATE.log" 2> "$LOGPATH/xbuildmk_stderr_$LOGDATE.log"
echo "xbuild: building done"

echo ""
echo "========================================"
echo "       Creating tar.gz archive"
echo "========================================"
echo ""

cd "MediaBrowser.Server.Mono/bin/Release Mono"

echo "Retreiving MediaBrowser version"
MBVERSION=$(monodis --assembly MediaBrowser.Server.Mono.exe | awk '$1 == "Version:" {print $2 }')

if [ -z "$MBVERSION" ]; then
    echo "Unable to get Mediabrowser version from monodis."
    exit
fi

echo "Creating MediaBrowser.Mono.mkbundlex.$MBVERSION.tar.gz"
if [ -e "$CWD/$BUILDPATH/MediaBrowser.Mono.mkbundlex.$MBVERSION.tar.gz" ]; then
    echo "Destination file exist: $CWD/$BUILDPATH/MediaBrowser.Mono.mkbundlex.$MBVERSION.tar.gz"
    exit
fi
mkdir "$CWD/$BUILDPATH/MediaBrowser.Mono.mkbundlex.$MBVERSION"
cp -fR * "$CWD/$BUILDPATH/MediaBrowser.Mono.mkbundlex.$MBVERSION/"
cd "$CWD/$BUILDPATH"

tar -czf "MediaBrowser.Mono.mkbundlex.$MBVERSION.tar.gz" "MediaBrowser.Mono.mkbundlex.$MBVERSION" > "$LOGPATH/tar_stdout_$LOGDATE.log" 2> "$LOGPATH/tar_stderr_$LOGDATE.log"

rm -rf "$CWD/$BUILDPATH/MediaBrowser.Mono.mkbundlex.$MBVERSION"

cd "$CWD"

echo ""
echo "========================================"
echo "                 Done"
echo "========================================"
