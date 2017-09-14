function [c,ceq]=nlcon(x,a,d,b)
ceq=[];
if b==-1
    c=[];
else
    c(1)=fcrit(d,x(1),x(2))-a(2);
    c(2)=a(1)-fcrit(d,x(1),x(2));
end

end



