help:
	@echo "This library is built using Visual Studio; see README.md"

.PHONY: doc docs clean cloc

cloc:
	@cloc --exclude-lang=XML WasatchNET WinFormDemo APITest LibUsbDotNetTest MonoTest UnitTests scripts

doc docs:
	@echo "Rendering Doxygen..."
	@rm -rf doc/doxygen
	@mkdir -p doc/doxygen
	@(cat Doxyfile ; echo "PROJECT_NUMBER = $$VERSION") | doxygen - 1>doxygen.out 2>doxygen.err
	@cat doxygen.out
	@cat doxygen.err

deploy:
	@for CLIENT in RamanSpecCal CrashTestNET ; \
     do test -d ../$$CLIENT && \
        cp -v lib/x86/WasatchNET.dll ../$$CLIENT/dist ; \
     done

clean:
	@rm -rf {WasatchNET,WinFormDemo,UnitTests,MultiChannelDemo,APITest,LibUsbDotNetTest}/{bin,obj} \
                                \
            Setup{32,64}/{Debug,Release} \
                                \
            lib/WasatchNET.dll  \
                                \
            doc/doxygen         \
            doxygen.{out,err}   \
                                \
            .vs
