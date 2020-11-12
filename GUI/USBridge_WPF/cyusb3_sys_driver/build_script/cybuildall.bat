@echo off

echo -e "\r"
echo -e "******************************************************\r"
echo -e "***  	Building Driver for all platfrom            ***\r"
echo -e "******************************************************\r"
echo -e "\r"

echo ********XP-X86-fre Build********
CMD /c cywdkbuild x86 fre wxp wxp

echo ********Vista-X86-fre Build********
CMD /c cywdkbuild x86 fre wlh vista

echo ********Vista-X64-fre Build********
CMD /c cywdkbuild x64 fre wlh vista

echo ********Win8.1-X86-fre Build********
CMD /c wdkbld_7_or_higher x86 fre Win8.1
echo ********Win8.1-X64-fre Build********
CMD /c wdkbld_7_or_higher x64 fre Win8.1


echo ********Win8-X86-fre Build********
CMD /c wdkbld_7_or_higher x86 fre Win8

echo ********Win8-X64-fre Build********
CMD /c wdkbld_7_or_higher x64 fre Win8



echo ********Win7-X86-fre Build********
CMD /c wdkbld_7_or_higher x86 fre Win7


echo ********Win7-X64-fre Build********
CMD /c wdkbld_7_or_higher x64 fre Win7


