clc
clear all



%% --------------------------------------------------------------------------
% read input files
f1='gh-actuation.csv';
f2='gh-glare.csv';
f3='gh-shade.csv';
f4='gh-view.csv';
f5='gh-seasonalCoefficients.csv';

%Reading small files
%actuation positions size:(number of actuation couples;NDOF)
%shading coefficients files size:(number of sun vectors;number of
%parameters for optimization)
ACT=csvread(f1);
SC=csvread(f5);

%Reading large files
%Glare GL, shading SH, view VW of size: (number of shades positions;number of sun vectors)
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

%the crit matrix stores the values of each parameter
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

disp('Quadratic function fitting started');

for i=1:ns

    disp(i);

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

    if plotfigs
        fig='figure '+num2str(i);
        Fig=figure;

        %
        subplot(3,1,1);
        scatter3(ACT(:,1),ACT(:,2),crit(:,i,1),'MarkerFaceColor',[0 .75 .75])
        axis([0 70 0 1 minscale maxscale])
        view(az,el)
%         ylabel('Extension (%)')
%         xlabel('Angle (�)')
%         zlabel('Normalized Shade (m2/m2)')
        hold on

        %
        subplot(3,1,2);
        scatter3(ACT(:,1),ACT(:,2),crit(:,i,2),'MarkerFaceColor',[0 .75 .75])
        axis([0 70 0 1 minscale maxscale])
%         view(az,el)
%         ylabel('Extension (%)')
%         xlabel('Angle (�)')
%         zlabel('Normalized Glare (m2/m2)')
        hold on

        %
        subplot(3,1,3);
        scatter3(ACT(:,1),ACT(:,2),crit(:,i,3),'MarkerFaceColor',[0 .75 .75])
        axis([0 70 0 1 minscale maxscale])
%         view(az,el)
%         ylabel('Extension (%)')
%         xlabel('Angle (�)')
%         zlabel('Normalized Shade (m2/m2)')
        hold on


        %
        subplot(3,1,1);
        syms x; syms y;ezsurf(c_opt(1,i,1)+c_opt(2,i,1)*x+c_opt(3,i,1)*y+c_opt(4,i,1)*x^2+c_opt(5,i,1)*x*y+c_opt(6,i,1)*y*y,[0,70],[0,1]);
        title('Shade')
        axis([0 70 0 1 minscale maxscale])
        view(az,el)
        ylabel('Extension (%)')
        xlabel('Angle (�)')
        zlabel('Normalized Shade (m2/m2)')

        %
        subplot(3,1,2);
        syms x; syms y;ezsurf(c_opt(1,i,2)+c_opt(2,i,2)*x+c_opt(3,i,2)*y+c_opt(4,i,2)*x^2+c_opt(5,i,2)*x*y+c_opt(6,i,2)*y*y,[0,70],[0,1]);
        title('Glare')
        axis([0 70 0 1 minscale maxscale])
        view(az,el)
        ylabel('Extension (%)')
        xlabel('Angle (�)')
        zlabel('Normalized Glare (m2/m2)')

        %
        subplot(3,1,3);
        syms x; syms y;ezsurf(c_opt(1,i,3)+c_opt(2,i,3)*x+c_opt(3,i,3)*y+c_opt(4,i,3)*x^2+c_opt(5,i,3)*x*y+c_opt(6,i,3)*y*y,[0,70],[0,1]);
        title('View')
        axis([0 70 0 1 minscale maxscale])
        view(az,el)
        ylabel('Extension (%)')
        xlabel('Angle (�)')
        zlabel('Normalized Shade (m2/m2)')
     end

end

% Store the result of the quadratic function fit into a csv file
csvwrite('fitQuad.csv',c_opt);

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


