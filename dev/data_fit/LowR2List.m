function [l,ind]=LowR2List(R2,lim,crit)

l=[];
ind=[];

for i=1:size(R2,2)
    if R2(crit,i)<=lim
       l=[l R2(crit,i)];
       ind=[ind i];
    end
end


end