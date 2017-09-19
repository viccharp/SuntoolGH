function [c,ceq]=nlcon(x,a,d,b)
ceq=[];
if b==-1
    c=[];
else
    c=a(1)-fcrit(d(:,:,1),x(1),x(2));
    %c(2)=(fcrit(d(:,:,2),x(1),x(2))-a(2));
end

end



