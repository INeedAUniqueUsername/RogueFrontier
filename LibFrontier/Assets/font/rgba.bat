for %%f in (*.png) do (
	ffmpeg -vcodec png -i %%f -vcodec rawvideo -f rawvideo -pix_fmt rgba %%~nf.rgba
)
pause