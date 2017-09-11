%% ------------------------------------------------------------------------

%definition of coefficients for balancing functions.
%temporarily, all coefficients are equal to one
c1=ones(ns,1);
c2=ones(ns,1);
c3=-ones(ns,1);
c=SC;

act_opt=zeros(ns,ndof);
fcrit_opt=zeros(ns,nc);
obj_opt=[];


X0=[70,1];
lb = [0,0];
ub = [70,1];
A_bal = [];
b_bal = [];
Aeq_bal = [];
beq_bal = [];


for i=1:ns

    fun=@(x)c(i,1)*fcrit(c_opt(:,i,1),x(1),x(2))+ c(i,2)*fcrit(c_opt(:,i,2),x(1),x(2))+c(i,3)*fcrit(c_opt(:,i,3),x(1),x(2));
    %ezsurf(c(i,1)*fcrit(c_opt(:,i,1),x,y)+ c(i,2)*fcrit(c_opt(:,i,2),x,y)+c(i,3)*fcrit(c_opt(:,i,3),x,y),[0,70],[0,1]);


    x = fmincon(fun,X0,A_bal,b_bal,Aeq_bal,beq_bal,lb,ub);


    act_opt(i,:)=x(1,:);
    fcrit_opt(i,:)=[fcrit(c_opt(:,i,1),x(1),x(2)), fcrit(c_opt(:,i,2),x(1),x(2)), fcrit(c_opt(:,i,3),x(1),x(2))];
    obj_opt(i)=c(i,1)*fcrit(c_opt(:,i,1),x(1),x(2))+ c(i,2)*fcrit(c_opt(:,i,2),x(1),x(2))+c(i,3)*fcrit(c_opt(:,i,3),x(1),x(2));
end

%%
csvwrite('outMatlab.csv',act_opt);
