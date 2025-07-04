@echo off
cd ..
py DZIP_Packer_v1.py -nsd -rot 22.5 -rmax 91 -a "track_content_2048.dz" -i %1 -no -ca zlib
echo.
echo Program will exit.
timeout 25