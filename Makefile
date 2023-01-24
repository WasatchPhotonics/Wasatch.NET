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
     do \
        for ARCH in x86 x64 SetupAnyCPU ; \
        do \
            if [ -d ../$$CLIENT/dist/$$ARCH ] ; \
            then \
                cp -v lib/$$ARCH/WasatchNET.dll ../$$CLIENT/dist/$$ARCH ; \
            fi ; \
        done ; \
     done

clean:
	@rm -rf {WasatchNET,WinFormDemo,UnitTests,MultiChannelDemo,APITest,LibUsbDotNetTest}/{bin,obj} \
                                \
            Setup{32,64,AnyCPU}/{Debug,Release} \
                                \
            lib/WasatchNET.dll  \
                                \
            doc/doxygen         \
            doxygen.{out,err}   \
                                \
            .vs
