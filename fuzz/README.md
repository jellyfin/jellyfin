# Jellyfin fuzzing

## Setup

Install AFL++
```sh
git clone https://github.com/AFLplusplus/AFLplusplus
cd AFLplusplus
make all
sudo make install
```

Install SharpFuzz.CommandLine global .NET tool
```sh
dotnet tool install --global SharpFuzz.CommandLine
```

## Running
Run the `fuzz.sh` in the directory corresponding to the project you want to fuzz.
The script takes a parameter of which fuzz case you want to run.
