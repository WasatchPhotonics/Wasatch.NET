help:
	@echo "This library is built using Visual Studio; see README.md"

.PHONY: doc clean

doc:
	@echo "Rendering Doxygen..."
	@doxygen 1>doxygen.out 2>doxygen.err

clean:
	@rm -rf WasatchNET/bin \
	        WasatchNET/obj \
	        WinFormDemo/obj \
	        WinFormDemo/bin \
            Setup/Release \
            Setup/Debug \
            lib/WasatchNET.dll \
            doc/doxygen/* \
            doxygen.* \
            .vs
