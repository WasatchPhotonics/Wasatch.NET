help:
	@echo "This library is built using Visual Studio; see README.md"

.PHONY: doc clean

doc:
	@echo "Rendering Doxygen..."
	@rm -rf doc/doxygen
	@mkdir -p doc/doxygen
	@(cat Doxyfile ; echo "PROJECT_NUMBER = $$VERSION") | doxygen - 1>doxygen.out 2>doxygen.err
	@cat doxygen.out
	@cat doxygen.err

clean:
	@rm -rf WasatchNET/bin      \
	        WasatchNET/obj      \
                                \
	        WinFormDemo/obj     \
	        WinFormDemo/bin     \
                                \
	        UnitTests/obj       \
	        UnitTests/bin       \
                                \
            Setup32/Release     \
            Setup32/Debug       \
                                \
            Setup64/Release     \
            Setup64/Debug       \
                                \
            lib/WasatchNET.dll  \
                                \
            doc/doxygen         \
            doxygen.out         \
            doxygen.err         \
                                \
            .vs
