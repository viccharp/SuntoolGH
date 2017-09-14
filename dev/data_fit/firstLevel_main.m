

%% ------------------------------------------------------------------------
% define input files
%seasonal coeficients
f1='gh-seasonalCoefficients.csv';
%quadratic function fitted to shade sample
f2='fitQuad.csv';
%summer/winter coefficent
f3='gh-permKron.csv';


c=csvread(f1);
fit_quad=csvread(f2);
kro=csvread(f3);

nsun=2386;
ncrit=size(fit_quad,2)/nsun;
ndof=2;
ncomb=size(c,1)/nsun;

c_opt=zeros(6,nsun,ncrit);
parameterCond=zeros(nsun,2,ncomb);

for i=1:(size(fit_quad,2))
    if mod(i,nsun)==0
        c_opt(:,nsun,ceil(i/nsun))=fit_quad(:,i);
    else
        c_opt(:,mod(i,nsun),ceil(i/nsun))=fit_quad(:,i);
    end
end


for i=1:size(c,1)
    if mod(i,nsun)==0
        parameterCond(nsun,:,ceil(i/nsun))=c(i,:);
    else
        parameterCond(mod(i,nsun),:,ceil(i/nsun))=c(i,:);
    end
end


%% --------------------------------------------------------------------------

act_opt=zeros(nsun,ndof,ncomb);
obj_opt=zeros(nsun,ncomb);


X0=[35,0.5];
lb = [0,0];
ub = [70,1];
A_bal = [];
b_bal = [];
Aeq_bal = [];
beq_bal = [];
%%
tic
ticBytes(gcp);
parfor i=1:ncomb
    for j=1:nsun
        
        fun=@(x)kro(j)*fcrit(c_opt(:,j,1),x(1),x(2));
   
        a=parameterCond(j,:,i);
        qf=c_opt(:,j,1);
        kron=kro(j);
        nonlcon=@(x)nlcon(x,a,qf,kron);
        x = fmincon(fun,X0,A_bal,b_bal,Aeq_bal,beq_bal,lb,ub,nonlcon);
        
        
        act_opt(j,:,i)=x(1,:);
        fcrit_opt(j,:,i)=[fcrit(c_opt(:,j,1),x(1),x(2)), fcrit(c_opt(:,j,2),x(1),x(2)), fcrit(c_opt(:,j,3),x(1),x(2))];
        obj_opt(j,i)=fcrit(c_opt(:,j,1),x(1),x(2));
    end
end
tocBytes(gcp);
toc
%%
csvwrite('outMatlab.csv',act_opt);































