clear all
clc

%% ------------------------------------------------------------------------
% define input files
%seasonal coeficients
f1='gh-seasonalCoefficients.csv';
%quadratic function fitted to shade sample
f2='fitQuad.csv';


c=csvread(f1);
fit_quad=csvread(f2);

nsun=2386;
ncrit=size(fit_quad,2)/nsun;
ndof=2;
ncomb=size(c,1)/nsun;

c_opt=zeros(6,nsun,ncrit);
coefs=zeros(nsun,3,ncomb);



modcheat=1;
for i=1:size(fit_quad,2)
    if mod(i,nsun)==0
        c_opt(:,modcheat,ceil(i/nsun))=fit_quad(:,i);
        modcheat=modcheat+1;
    else
        c_opt(:,mod(i,nsun),ceil(i/nsun))=fit_quad(:,i);
    end
end

mmodcheat=1;
for i=1:size(c,1)
    if mod(i,nsun)==0
        coefs(mmodcheat,:,ceil(i/nsun))=c(i,:);
        mmodcheat=mmodcheat+1;
    else
        coefs(mod(i,nsun),:,ceil(i/nsun))=c(i,:);
    end
end




%% --------------------------------------------------------------------------

act_opt=zeros(nsun,ndof,ncomb);
fcrit_opt=zeros(nsun,ncrit,ncomb);
obj_opt=zeros(nsun,ncomb);


X0=[35,0.5];
lb = [0,0];
ub = [70,1];
A_bal = [];
b_bal = [];
Aeq_bal = [];
beq_bal = [];

tic
ticBytes(gcp);
parfor i=1:ncomb
    for j=1:nsun
        
        fun=@(x)coefs(j,1,i)*fcrit(c_opt(:,j,1),x(1),x(2))+ coefs(j,2,i)*fcrit(c_opt(:,j,2),x(1),x(2))+coefs(j,3,i)*fcrit(c_opt(:,j,3),x(1),x(2));
        %ezsurf(c(i,1)*fcrit(c_opt(:,i,1),x,y)+ c(i,2)*fcrit(c_opt(:,i,2),x,y)+c(i,3)*fcrit(c_opt(:,i,3),x,y),[0,70],[0,1]);
        
        
        x = fmincon(fun,X0,A_bal,b_bal,Aeq_bal,beq_bal,lb,ub);
        
        
        act_opt(j,:,i)=x(1,:);
        fcrit_opt(j,:,i)=[fcrit(c_opt(:,j,1),x(1),x(2)), fcrit(c_opt(:,j,2),x(1),x(2)), fcrit(c_opt(:,j,3),x(1),x(2))];
        obj_opt(j,i)=coefs(j,1,i)*fcrit(c_opt(:,j,1),x(1),x(2))+ coefs(j,2,i)*fcrit(c_opt(:,j,2),x(1),x(2))+coefs(j,3,i)*fcrit(c_opt(:,j,3),x(1),x(2));
    end
end
tocBytes(gcp);
toc
%%
csvwrite('outMatlab.csv',act_opt);
