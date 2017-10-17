
VSUN=[1 2 1000 1001 1002];


for i=1:size(VSUN,2)
    Fig=figure;
    
    suptitle(int2str(VSUN(i)));
    
    
    for j=1:3        
        %Raw data contour 
        currentCrit=reshape(crit(:,VSUN(i),j),9,9);
        subplot(3,3,3*j-2);
        if j==1
            [C,h]=contourf(currentCrit');
            clabel(C,h);
        else
            [C,h]=contourf(currentCrit',[0,0.05,0.1,0.3,0.5,0.7,0.9,0.95,1]);
            clabel(C,h);
        end
        h.XData=[0.01,8.75,17.5,26.25,35,43.75,52.5,64.25,70];
        h.YData=[0.01,0.125,0.25,0.375,0.50,0.625,0.75,0.875,1];
        view(-90,90);
        colorbar;
        xlabel('Angle (Deg)','fontsize',14);
        ylabel('Extension (%)','fontsize',14);
        clear C;
        clear h;
        hold on;
        
        %Raw data scattered       
        subplot(3,3,3*j-1);
        scatter3(ACT(:,1),ACT(:,2),crit(:,VSUN(i),j),'MarkerFaceColor',[0 .75 .75])
        if j==1
            axis([0 70 0 1 0 8])
        else
            axis([0 70 0 1 0 1])
        end
        xlabel('Angle (Deg)','fontsize',14);
        ylabel('Extension (%)','fontsize',14);
        view(-75,33)
        hold on;
    
        
        %Function fit over raw data
        subplot(3,3,3*j);
        scatter3(ACT(:,1),ACT(:,2),crit(:,VSUN(i),j),'MarkerFaceColor',[0 .75 .75])
        hold on
        syms x; syms y;ezsurf(c_opt(1,VSUN(i),j)+c_opt(2,VSUN(i),j)*x+c_opt(3,VSUN(i),j)*y+c_opt(4,VSUN(i),j)*x^2+c_opt(5,VSUN(i),j)*x*y+c_opt(6,VSUN(i),j)*y*y,[0,70],[0,1]);
        if j==1
            axis([0 70 0 1 0 8])
        else
            axis([0 70 0 1 0 1])
        end
        xlabel('Angle (Deg)','fontsize',14);
        ylabel('Extension (%)','fontsize',14);
        view(-75,33);
        title(R2(VSUN(i),j));
        hold on

    end
    
end
 