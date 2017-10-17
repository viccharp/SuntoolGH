clc
clear all



%% --------------------------------------------------------------------------
% read input files
f1='gh-actuation.csv';
f2='gh-deskGlare.csv';
f3='gh-solarGain.csv';
f4='gh-view.csv';
% f5='gh-seasonalCoefficients.csv';

%Reading small files
%actuation positions size:(number of actuation couples;NDOF)
%shading coefficients files size:(number of sun vectors;number of
%parameters for optimization)
ACT=csvread(f1);
% SC=csvread(f5);

%Reading large files
%Glare GL, shading SH, view VW of size: (number of shades positions;number of sun vectors)
DGL=csvread(f2);
SG=csvread(f3);
VW=csvread(f4);


%% --------------------------------------------------------------------------
% check for size uniformity, define number of sun vector and criteria
% matrix
if (size(SG,2)==size(DGL,2))&&(size(SG,2)==size(VW,2))
    ns=size(SG,2);
else
    error('error in number of sun vectors in input')
end

ncrit=3; %number of criteria for the optimization
nact=size(ACT,1);
ndof=size(ACT,2);

%the crit matrix stores the values of each parameter
crit(:,:,1)=SG;
crit(:,:,2)=VW;
crit(:,:,3)=DGL;

%maxscale=max(crit(:,1,3));
%minscale=min(crit(:,1,3));

%% --------------------------------------------------------------------------
% Main loop

%storage of optimization results
%coefficients of the quadratic function a1+a2*x+a3*y+a4*x^2+a5*x*y+a6*y^2
c_opt=zeros(6,ns,ncrit);

plotfigs=false;
az=135;
el=45;

disp('Quadratic function fitting started');

tic

for i=1:ns

    disp(i);

    %
    cvx_begin quiet
    variables a(6,1)
    minimize (sum((crit(:,i,1)-a(1)-a(2)*ACT(:,1)-a(3)*ACT(:,2)-a(4)*ACT(:,1).^2-a(5)*ACT(:,1).*ACT(:,2)-a(6)*ACT(:,2).*ACT(:,2)).^2))
    [a(4) a(5)/2; a(5)/2 a(6)]==semidefinite(2);
    subject to 
        a(1)+a(2)*ACT(:,1)+a(3)*ACT(:,2)+a(4)*ACT(:,1).^2+a(5)*ACT(:,1).*ACT(:,2)+a(6)*ACT(:,2).*ACT(:,2)>=0;
        
    cvx_end
    c_opt(:,i,1)=a(:,1);
    a(:,1)=zeros(6,1);

    %
    cvx_begin quiet
    variables a(6,1)
    minimize (sum((crit(:,i,2)-a(1)-a(2)*ACT(:,1)-a(3)*ACT(:,2)-a(4)*ACT(:,1).^2-a(5)*ACT(:,1).*ACT(:,2)-a(6)*ACT(:,2).*ACT(:,2)).^2))
    [a(4) a(5)/2; a(5)/2 a(6)]==semidefinite(2);
    subject to 
        a(1)+a(2)*ACT(:,1)+a(3)*ACT(:,2)+a(4)*ACT(:,1).^2+a(5)*ACT(:,1).*ACT(:,2)+a(6)*ACT(:,2).*ACT(:,2)>=0;
        a(1)+a(2)*ACT(:,1)+a(3)*ACT(:,2)+a(4)*ACT(:,1).^2+a(5)*ACT(:,1).*ACT(:,2)+a(6)*ACT(:,2).*ACT(:,2)<=1;
    cvx_end
    c_opt(:,i,2)=a(:,1);
    a(:,1)=zeros(6,1);

    %
    cvx_begin quiet
    variables a(6,1)
    minimize (sum((crit(:,i,3)-a(1)-a(2)*ACT(:,1)-a(3)*ACT(:,2)-a(4)*ACT(:,1).^2-a(5)*ACT(:,1).*ACT(:,2)-a(6)*ACT(:,2).*ACT(:,2)).^2))
    [a(4) a(5)/2; a(5)/2 a(6)]==semidefinite(2);
    subject to 
        a(1)+a(2)*ACT(:,1)+a(3)*ACT(:,2)+a(4)*ACT(:,1).^2+a(5)*ACT(:,1).*ACT(:,2)+a(6)*ACT(:,2).*ACT(:,2)>=0;
        a(1)+a(2)*ACT(:,1)+a(3)*ACT(:,2)+a(4)*ACT(:,1).^2+a(5)*ACT(:,1).*ACT(:,2)+a(6)*ACT(:,2).*ACT(:,2)<=1;
    cvx_end
    c_opt(:,i,3)=a(:,1);
    a(:,1)=zeros(6,1);

end

toc

% % Store the result of the quadratic function fit into a csv file
% csvwrite('fitQuad.csv',c_opt);

%% ------------------------------------------------------------------------
% R-squared
Rs=[];


crit_avg=zeros(ncrit,ns);
ds=zeros(ns,nact);
df=zeros(ns,nact);
SST=zeros(ncrit,ns);
SSRS=zeros(ncrit,ns);
R2=zeros(ncrit,ns);

for j=1:ncrit
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

%% 

for i=1:size(R2(3,:),2)
    if R2(3,i)<0
        R2(3,i)=0;
    end
end
%% 
[llr,illr]=LowR2List(R2,0.80,3);
mr23=mean(R2(3,:))
sdr23=std(R2(3,:))
qtr23=quantile(R2(3,:),0.8795)
mdr23=median(R2(3,:))


%% 

% %% Max min of the fitted functions for scaling
% mm=zeros(ncrit,2,ns);
% ninter=1000;
% inter1=(70-0)/ninter;
% inter2=(1-0)/ninter;
% maxAct=zeros(ncrit,2,ns);
% minAct=zeros(ncrit,2,ns);
% for k=1:ns
%     for i=1:ncrit
%         max=-9990;
%         min=9990;
%         %         syms x;
%         %         syms y;
%         %         fun=@(x,y)fcrit(c_opt(:,k,i),x,y);
%         %
%         %         mm(i,1,k)=min(double(fun));
%         %         %mm(i,2,k)=
%         
%         for j=1:ninter
%             for m=1:ninter
%                 current_val=fcrit(c_opt(:,k,i),inter1*j,inter2*m);
%                 if current_val>max
%                     max=current_val;
%                     maxAct(i,1,k)=inter1*j;
%                     maxAct(i,2,k)=inter2*j;
%                 end
%                 if current_val<min
%                     min=current_val;
%                     minAct(i,1,k)=inter1*j;
%                     minAct(i,2,k)=inter2*j;
%                 end
%             end
%         end
%         mm(i,1,k)=min;
%         mm(i,2,k)=max;
%     end
% end
% 
% csvwrite('bounds.csv',mm);
%     
    




