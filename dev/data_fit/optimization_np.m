clc
clear all



%% --------------------------------------------------------------------------
% read input files
f1='actuation.csv';
f2='glare.csv';
f3='shade.csv';
f4='view.csv';

ACT=csvread(f1);
GL=csvread(f2);
SH=csvread(f3);
VW=csvread(f4);

%% --------------------------------------------------------------------------
% check for size uniformity, define number of sun vector and criteria
% matrix
if (size(SH,2)==size(GL,2))&&(size(SH,2)==size(VW,2))
    ns=size(SH,2);
else
    error('error in number of sun vectors in input')
end

nc=3; %number of criteria for the optimization
nact=size(ACT,1);
ndof=size(ACT,2);

crit(:,:,1)=SH;
crit(:,:,2)=GL;
crit(:,:,3)=VW;

maxscale=max(crit(:,1,3));
minscale=min(crit(:,1,3));

%% --------------------------------------------------------------------------
% Main loop

%storage of optimization results
%coefficients of the quadratic function a1+a2*x+a3*y+a4*x^2+a5*x*y+a6*y^2
c_opt=zeros(6,ns,nc);

plotfigs=false;
az=135;
el=45;

for i=1:ns
  
    %
    cvx_begin quiet
    variables a(6,1)
    minimize (sum((crit(:,i,1)-a(1)-a(2)*ACT(:,1)-a(3)*ACT(:,2)-a(4)*ACT(:,1).^2-a(5)*ACT(:,1).*ACT(:,2)-a(6)*ACT(:,2).*ACT(:,2)).^2))
    [a(4) a(5)/2; a(5)/2 a(6)]==semidefinite(2);
    cvx_end
    c_opt(:,i,1)=a(:,1);
    a(:,1)=zeros(6,1);
    
    %   
    cvx_begin quiet
    variables a(6,1)
    minimize (sum((crit(:,i,2)-a(1)-a(2)*ACT(:,1)-a(3)*ACT(:,2)-a(4)*ACT(:,1).^2-a(5)*ACT(:,1).*ACT(:,2)-a(6)*ACT(:,2).*ACT(:,2)).^2))
    [a(4) a(5)/2; a(5)/2 a(6)]==semidefinite(2);
    cvx_end
    c_opt(:,i,2)=a(:,1);
    a(:,1)=zeros(6,1);
    
    %
    cvx_begin quiet
    variables a(6,1)
    minimize (sum((crit(:,i,3)-a(1)-a(2)*ACT(:,1)-a(3)*ACT(:,2)-a(4)*ACT(:,1).^2-a(5)*ACT(:,1).*ACT(:,2)-a(6)*ACT(:,2).*ACT(:,2)).^2))
    [a(4) a(5)/2; a(5)/2 a(6)]==semidefinite(2);
    cvx_end
    c_opt(:,i,3)=a(:,1);
    a(:,1)=zeros(6,1);

end

%% ------------------------------------------------------------------------
% R-squared
Rs=[];


crit_avg=zeros(nc,ns);
ds=zeros(ns,nact);
df=zeros(ns,nact);
SST=zeros(nc,ns);
SSRS=zeros(nc,ns);
R2=zeros(nc,ns);

for j=1:nc
    crit_avg(j,:)=sum(crit(:,:,j))/nact;
    for i=1:ns
       dss=0;
       dff=0;
       for k=1:nact
           x=ACT(k,1);
           y=ACT(k,2);
           fi=c_opt(1,i,j)+c_opt(2,i,j)*x+c_opt(3,i,j)*y+c_opt(4,i,j)*x^2+c_opt(5,i,j)*x*y+c_opt(6,i,j)*y*y;
           ds(i,k)=(crit(k,i,j)-crit_avg(j,i))^2;
           df(i,k)=(crit(k,i,j)-fi)^2;
           dss=dss+ds(i,k);
           dff=dff+df(i,k);
       end
       SST(j,i)=dss;
       SSRS(j,i)=dff;
       R2(j,i)=1-dff/dss;
    end
end

%% ------------------------------------------------------------------------

%definition of coefficients for balancing functions.
%temporarily, all coefficients are equal to one
c1=ones(ns,1);
c2=ones(ns,1);
c3=-ones(ns,1);
c=[c1 c2 c3];

act_opt=zeros(ns,ndof);
fcrit_opt=zeros(ns,nc);
obj_opt=[];


X0=[0,0];
lb = [0,0];
ub = [70,1];
A_bal = [];
b_bal = [];
Aeq_bal = [];
beq_bal = [];

for i=1:ns
    
    fun=@(x)c(i,1)*fcrit(c_opt(:,i,1),x(1),x(2))+ c(i,2)*fcrit(c_opt(:,i,2),x(1),x(2))+c(i,3)*fcrit(c_opt(:,i,3),x(1),x(2));
    
    x = fmincon(fun,X0,A_bal,b_bal,Aeq_bal,beq_bal,lb,ub);
    
    act_opt(i,:)=x(1,:);
    fcrit_opt(i,:)=[fcrit(c_opt(:,i,1),x(1),x(2)), fcrit(c_opt(:,i,2),x(1),x(2)), fcrit(c_opt(:,i,3),x(1),x(2))];
    obj_opt(i)=c(i,1)*fcrit(c_opt(:,i,1),x(1),x(2))+ c(i,2)*fcrit(c_opt(:,i,2),x(1),x(2))+c(i,3)*fcrit(c_opt(:,i,3),x(1),x(2));
end
         
%%
csvwrite('outMatlab.csv',act_opt);



























