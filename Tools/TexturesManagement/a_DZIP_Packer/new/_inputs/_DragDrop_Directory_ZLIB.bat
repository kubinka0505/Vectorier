@echo off
cd ..
py DZIP_Packer_v2.py -nsd -a "track_content_2048.dz" -i %1 -ca zlib -no
echo.
echo Program will exit.
timeout 25