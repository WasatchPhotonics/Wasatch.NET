help:
	@echo "This library is built using Visual Studio; see README.md"

clean:
	@rm -rf WasatchNET/bin \
	        WasatchNET/obj \
	        WinFormDemo/obj \
	        WinFormDemo/bin \
            Setup/Release \
            Setup/Debug \
            lib/WasatchNET.dll \
            .vs
