function result = parseRanges(ipString,min,max)
%Interprets ranges and values to return a list of desired elements.
% Input string cleaning is done, not 100% sure how robust
%  Example string in is: "1,5-9,4,12"  then output is vector with 1,4,5,6,7,8,9,12


a = regexprep(ipString,'[^\d-,]',''); %strip out everything not a digit or - or ,
b = regexprep(a,'(,-)|(-,)',','); % remove invalid range specifications
c = regexprep(b,',,',','); % remove empty
d =eval(cat(2,'[',strrep(c,'-',':'),']')); % get matlab to interpret it
e = sort(d); %sort the results
f = unique(e); % remove duplication
result = f((f>=min)&(f<=max)); % limit the acceptable range


end