using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Primitives3D;

namespace Game.Newt.Testers
{
    public partial class Curves : Window
    {
        #region Class: AxeSimple1

        private class AxeSimple1
        {
            public AxeSimple1()
            {
                Random rand = StaticRandom.GetRandomForThread();

                this.isCenterFilled = rand.NextBool();

                // Edge
                this.edgeAngle = rand.NextPercent(45, 1);
                this.edgePercent = rand.NextPercent(.33, .75);

                // Left
                this.leftAway = rand.NextBool();

                if (this.leftAway)
                {
                    this.leftAngle = rand.NextDouble(0, 15);
                    this.leftPercent = rand.NextPercent(.25, .5);
                }
                else
                {
                    this.leftAngle = rand.NextPercent(20, .5);
                    this.leftPercent = rand.NextPercent(.25, .75);
                }

                // Right
                this.rightAway = this.leftAway ? true : rand.NextBool();        // it looks like a vase when left is away, and right is toward

                if (this.rightAway)
                {
                    this.rightAngle = rand.NextDouble(0, 15);
                    this.rightPercent = rand.NextPercent(.25, .5);
                }
                else
                {
                    this.rightAngle = rand.NextPercent(20, .75);
                    this.rightPercent = rand.NextPercent(.25, .75);
                }

                // Points
                this.leftX = rand.NextDouble(1, 3);
                if (this.leftAway)
                {
                    this.leftY = rand.NextDouble(2, 2.5);
                }
                else
                {
                    this.leftY = rand.NextDouble(1.25, 2.5);
                }

                this.rightX = 2;
                this.rightY = 3.4;

                // Z
                this.Scale1X = rand.NextDouble(.6, .8);
                this.Scale1Y = rand.NextDouble(.8, .95);
                this.Z1 = rand.NextPercent(.2, .5);

                this.Scale2X = rand.NextPercent(.4, .25);
                this.Scale2Y = rand.NextPercent(.4, .25);
                this.Z2L = rand.NextPercent(.55, .25);
                this.Z2R = rand.NextPercent(.33, .25);
            }

            public double leftX;
            public double leftY;
            public double rightX;
            public double rightY;

            public double edgeAngle;
            public double edgePercent;

            public double leftAngle;
            public double leftPercent;
            public bool leftAway;

            public double rightAngle;
            public double rightPercent;
            public bool rightAway;

            public bool isCenterFilled;

            public double Scale1X;
            public double Scale1Y;
            public double Z1;

            public double Scale2X;
            public double Scale2Y;
            public double Z2L;
            public double Z2R;
        }

        #endregion
        #region Class: AveragePlaneTests

        private class AveragePlaneTests
        {
            #region VARIOUS ATTEMPTS

            #region WRONG
            //public static ITriangle GetAveragePlane(Point3D[] points)
            //{
            //    Triangle plane = GetAveragePlane_Initial(points);
            //    if (plane == null)
            //    {
            //        return null;
            //    }

            //    // Make sure the plane goes through the center of position
            //    Point3D center = Math3D.GetCenter(points);

            //    // Rotate the plane, trying to reduce the amount of off plane error
            //    for (int cntr = 0; cntr < 2; cntr++)
            //    {
            //        //plane = GetAveragePlane_Rotate(plane, center, points);
            //    }

            //    return plane;
            //}
            //private static Triangle GetAveragePlane_Initial(Point3D[] points)
            //{
            //    Vector3D? line1 = null;
            //    Vector3D? line1Unit = null;

            //    for (int cntr = 1; cntr < points.Length; cntr++)
            //    {
            //        if (Math3D.IsNearValue(points[0], points[cntr]))
            //        {
            //            // These points are sitting on top of each other
            //            continue;
            //        }

            //        Vector3D line = points[cntr] - points[0];

            //        if (line1 == null)
            //        {
            //            // Found the first line
            //            line1 = line;
            //            line1Unit = line.ToUnit();
            //            continue;
            //        }

            //        if (!Math3D.IsNearValue(Math.Abs(Vector3D.DotProduct(line1Unit.Value, line.ToUnit())), 1d))
            //        {
            //            // These two lines aren't colinear.  Found the second line
            //            return new Triangle(points[0], points[0] + line1.Value, points[cntr]);
            //        }
            //    }

            //    return null;
            //}
            #endregion
            #region RELIES ON MATLAB

            //import numpy as np
            //import scipy.optimize

            #region fitPLaneLTSQ
            //def fitPLaneLTSQ(XYZ):
            //    # Fits a plane to a point cloud, 
            //    # Where Z = aX + bY + c        ----Eqn #1
            //    # Rearanging Eqn1: aX + bY -Z +c =0
            //    # Gives normal (a,b,-1)
            //    # Normal = (a,b,-1)
            //    [rows,cols] = XYZ.shape
            //    G = np.ones((rows,3))
            //    G[:,0] = XYZ[:,0]  #X
            //    G[:,1] = XYZ[:,1]  #Y
            //    Z = XYZ[:,2]
            //    (a,b,c),resid,rank,s = np.linalg.lstsq(G,Z) 
            //    normal = (a,b,-1)
            //    nn = np.linalg.norm(normal)
            //    normal = normal / nn
            //    return normal
            #endregion

            //def fitPlaneSVD(XYZ):
            //    [rows,cols] = XYZ.shape
            //    //# Set up constraint equations of the form  AB = 0,
            //    //# where B is a column vector of the plane coefficients
            //    //# in the form b(1)*X + b(2)*Y +b(3)*Z + b(4) = 0.
            //    p = (np.ones((rows,1)))
            //    AB = np.hstack([XYZ,p])
            //    [u, d, v] = np.linalg.svd(AB,0)        
            //    B = v[3,:];                    # Solution is last column of v.
            //    nn = np.linalg.norm(B[0:3])
            //    B = B / nn
            //    return B[0:3]


            #region fitPlaneEigen
            //def fitPlaneEigen(XYZ):
            //    # Works, in this case but don't understand!
            //    average=sum(XYZ)/XYZ.shape[0]
            //    covariant=np.cov(XYZ - average)
            //    eigenvalues,eigenvectors = np.linalg.eig(covariant)
            //    want_max = eigenvectors[:,eigenvalues.argmax()]
            //    (c,a,b) = want_max[3:6]    # Do not understand! Why 3:6? Why (c,a,b)?
            //    normal = np.array([a,b,c])
            //    nn = np.linalg.norm(normal)
            //    return normal / nn  
            #endregion
            #region fitPlaneSolve
            //def fitPlaneSolve(XYZ):
            //    X = XYZ[:,0]
            //    Y = XYZ[:,1]
            //    Z = XYZ[:,2] 
            //    npts = len(X)
            //    A = np.array([ [sum(X*X), sum(X*Y), sum(X)],
            //                   [sum(X*Y), sum(Y*Y), sum(Y)],
            //                   [sum(X),   sum(Y), npts] ])
            //    B = np.array([ [sum(X*Z), sum(Y*Z), sum(Z)] ])
            //    normal = np.linalg.solve(A,B.T)
            //    nn = np.linalg.norm(normal)
            //    normal = normal / nn
            //    return normal.ravel()
            #endregion
            #region fitPlaneOptimize
            //def fitPlaneOptimize(XYZ):
            //    def residiuals(parameter,f,x,y):
            //        return [(f[i] - model(parameter,x[i],y[i])) for i in range(len(f))]


            //    def model(parameter, x, y):
            //        a, b, c = parameter
            //        return a*x + b*y + c

            //    X = XYZ[:,0]
            //    Y = XYZ[:,1]
            //    Z = XYZ[:,2]
            //    p0 = [1., 1.,1.] # initial guess
            //    result = scipy.optimize.leastsq(residiuals, p0, args=(Z,X,Y))[0]
            //    normal = result[0:3]
            //    nn = np.linalg.norm(normal)
            //    normal = normal / nn
            //    return normal
            #endregion

            //if __name__=="__main__":
            //    XYZ = np.array([
            //        [0,0,1],
            //        [0,1,2],
            //        [0,2,3],
            //        [1,0,1],
            //        [1,1,2],
            //        [1,2,3],
            //        [2,0,1],
            //        [2,1,2],
            //        [2,2,3]
            //        ])
            //    print "Solve: ", fitPlaneSolve(XYZ)
            //    print "Optim: ",fitPlaneOptimize(XYZ)
            //    print "SVD:   ",fitPlaneSVD(XYZ)
            //    print "LTSQ:  ",fitPLaneLTSQ(XYZ)
            //    print "Eigen: ",fitPlaneEigen(XYZ)

            #endregion


            //http://www.timzaman.com/?p=190
            #region local ransac

            //% Tim Zaman, 2010, TU Delft, MSc:ME:BME:BMD:BR
            //%
            //% This local_ransac algorythm uses the 'no' amount of closest variables to
            //%  the randomly chosen point:
            //%
            //% Usage:
            //% [p_best,n_best,ro_best,X_best,Y_best,Z_best,error_best,sample_best]=local_ransac_tim(p,no,k,t,d)
            //%
            //% no smallest number of points required
            //% k number of iterations
            //% t threshold used to id a point that fits well
            //% d number of nearby points required


            //function [p_best,n_best,ro_best,X_best,Y_best,Z_best,error_best,sample_best]=local_ransac_tim(p,no,k,t,d)


            //succes=0;
            //%Make sure we get a result =) (yeah i know its not common for RANSAC)
            //while succes==0
            //    %Initialize variables
            //    iterations=0;
            //    %Until k iterations have occurrec
            //    while iterations < k
            //        ii=0;
            //        clear p_close dist p_new p_in p_out sample_in loc_dist sample_out

            //        %Pick a point
            //        sample_in(1)=randi(length(p)); %point location
            //        p_in(1,:)=p(sample_in(1),:); %point value

            //        %Compute all local distances to this point
            //        loc_dist=sqrt((p_in(1)-p(:,1)).^2+(p_in(2)-p(:,2)).^2+(p_in(3)-p(:,3)).^2);

            //        %Initialize sample out
            //        sample_out=[1:1:length(p)];

            //        %Exclude the sample_in's
            //        sample_out=sample_out(sample_out~=sample_in(1)); %remove first

            //        for n=1:no

            //            %Make sure the first sample can not be chosen anymore
            //            loc_dist(sample_in(n))=inf;
            //            [~,sample_in(n+1)]=min(loc_dist);     %point location
            //            p_in(n+1,:)=p(sample_in(n+1),:);        %point value

            //            %Exclude the sample_in's
            //            sample_out=sample_out(sample_out~=sample_in(n+1)); %remove

            //        end


            //        %Fit to that set of n points
            //        [n_est_in ro_est_in]=LSE(p_in);

            //        p_new=p_in;

            //        %For each data point oustide the sample
            //        for i=sample_out
            //            dist=dot(n_est_in,p(i,:))-ro_est_in;
            //            %Test distance d to t
            //            abs(dist);
            //            if abs(dist)<t %If d<t, the point is close
            //                ii=ii+1;
            //                %p_close(ii,:)=;
            //                p_new=[p_new;p(i,:)];
            //                %And remember the location of this succesful number and add
            //                sample_in=[sample_in i];
            //            end
            //        end


            //        %If there are d or more points close to the line
            //        if length(p_new) > d
            //            %Refit the line using all these points
            //            [n_est_new ro_est_new X Y Z]=LSE(p_new);
            //            for iii=1:length(p_new)
            //                dist(iii)=dot(n_est_new,p_new(iii,:))-ro_est_new;
            //            end
            //            %Use the fitting error as error criterion (ive used SAD for ease)
            //            error(iterations+1)=sum(abs(dist));
            //        else
            //            error(iterations+1)=inf;
            //        end

            //        if length(p_new) > d %made this up myself too
            //            if iterations >1
            //                %Use the best fit from this collection
            //                if error(iterations+1) <= min(error)
            //                    succes=1;
            //                    p_best=p_new;
            //                    n_best=n_est_new;
            //                    ro_best=ro_est_new;
            //                    X_best=X;
            //                    Y_best=Y;
            //                    Z_best=Z;
            //                    error_best=error(iterations+1);
            //                    sample_best=sample_in;
            //                end
            //            end
            //        end

            //        iterations=iterations+1;
            //    end
            //end


            //end

            #endregion
            #region ransac
            //% 
            //%no smallest number of points required
            //%k number of iterations
            //%t threshold used to id a point that fits well
            //%d number of nearby points required


            //function [p_best,n_best,ro_best,X_best,Y_best,Z_best,error_best]=ransac_tim(p,no,k,t,d)

            //%Initialize variables
            //iterations=0;
            //%Until k iterations have occurrec
            //while iterations < k
            //    ii=0;
            //    clear p_close dist p_new p_in p_out

            //    %Draw a sample of n points from the data
            //    perm=randperm(length(p));
            //    sample_in=perm(1:no);
            //    p_in=p(sample_in,:);
            //    sample_out=perm(no+1:end);
            //    p_out=p(sample_out,:);

            //    %Fit to that set of n points
            //    [n_est_in ro_est_in]=LSE(p_in);

            //    %For each data point oustide the sample
            //    for i=sample_out
            //        dist=dot(n_est_in,p(i,:))-ro_est_in;
            //        %Test distance d to t
            //        abs(dist);
            //        if abs(dist)<t %If d<t, the point is close
            //            ii=ii+1;
            //            p_close(ii,:)=p(i,:);
            //        end
            //    end

            //    p_new=[p_in;p_close];

            //    %If there are d or more points close to the line
            //    if length(p_new) > d
            //        %Refit the line using all these points
            //        [n_est_new ro_est_new X Y Z]=LSE(p_new);
            //        for iii=1:length(p_new)
            //            dist(iii)=dot(n_est_new,p_new(iii,:))-ro_est_new;
            //        end
            //        %Use the fitting error as error criterion (ive used SAD for ease)
            //        error(iterations+1)=sum(abs(dist));
            //    else
            //        error(iterations+1)=inf;
            //    end


            //    if iterations >1
            //        %Use the best fit from this collection 
            //        if error(iterations+1) <= min(error)
            //            p_best=p_new;
            //            n_best=n_est_new;
            //            ro_best=ro_est_new;
            //            X_best=X;
            //            Y_best=Y;
            //            Z_best=Z;
            //            error_best=error(iterations+1);
            //        end
            //    end

            //    iterations=iterations+1;
            //end


            //end
            #endregion
            #region example
            //%Tim Zaman (1316249), 2010, TU Delft, MSc:ME:BME:BMD:BR
            //clc
            //close all
            //clear all

            //%Define variables
            //nrs=100; %amount of numbers
            //maxnr=20; %max amount of X or Y

            //n(1,:)=[1 1 1];  %1st plane normal vector
            //n(2,:)=[1 1 -1]; %2nd plane normal vector
            //n(3,:)=[-1 0 1]; %3rd plane normal vector
            //n(4,:)=[1 -1 1]; %4th plane normal vector
            //ro=[10 20 10 40];

            //%Make random points...
            //for i=1:4 %for 4 different planes

            //%Declarate 100 random x's and y's (p1 and p2)
            //p(1:nrs,1:2,i)=randi(maxnr,nrs,2);
            //%From the previous, calculate the Z
            //p(1:nrs,3,i)=(ro(i)-n(i,1)*p(1:nrs,1,i)-n(i,2).*p(1:nrs,2,i))/n(i,3);

            //%Add some random points
            //for ii=1:20 %10 points
            //randpt=p(randi(nrs),1:3,i);  %take an random existing point
            //randvar=(randi(14,1,3)-7);       %adapt that randomly +-7
            //p(nrs+ii,1:3,i)=randpt+randvar; %put behind pointmatrix
            //end

            //end

            //%combine the four dataset-planes we have made into one
            //p_tot=[p(:,:,1);p(:,:,2);p(:,:,3);p(:,:,4)];

            //figure
            //plot3(p(:,1,1),p(:,2,1),p(:,3,1),'.r')
            //hold on; grid on
            //plot3(p(:,1,2),p(:,2,2),p(:,3,2),'.g')
            //plot3(p(:,1,3),p(:,2,3),p(:,3,3),'.b')
            //plot3(p(:,1,4),p(:,2,4),p(:,3,4),'.k')

            //no=3;%smallest number of points required
            //k=5;%number of iterations
            //t=2;%threshold used to id a point that fits well
            //d=70;%number of nearby points required

            //%Initialize samples to pick from
            //samples_pick=[1:1:length(p_tot)];
            //p_pick=p_tot;
            //for i=1:4 %Search for 4 planes

            //[p_best,n_best,ro_best,X_best,Y_best,Z_best,error_best,samples_used]=local_ransac_tim(p_pick,no,k,t,d);

            //samples_pick=[1:1:length(p_pick)];

            //%Remove just used points from points for next plane
            //for ii=1:length(samples_used) %In lack for a better way to do it used a loop
            //samples_pick=samples_pick(samples_pick~=samples_used(ii)); %remove first
            //end

            //p_pick=p_pick(samples_pick,:);

            //pause(.5)
            //plot3(p_best(:,1),p_best(:,2),p_best(:,3),'ok')
            //mesh(X_best,Y_best,Z_best);colormap([.8 .8 .8])
            //beep
            //end
            #endregion

            #region ransac (c#)

            //// no smallest number of points required
            //// k number of iterations
            //// t threshold used to id a point that fits well
            //// d number of nearby points required


            ////function [p_best,n_best,ro_best,X_best,Y_best,Z_best,error_best]=ransac_tim(p,no,k,t,d)
            //public static Vector3D ransac(Point3D[] p, int no, int k, double t, int d)
            //{
            //    Random rand = StaticRandom.GetRandomForThread();

            //    Point3D[] p_best;       // = ??????
            //    //n_best,ro_best,X_best,Y_best,Z_best,
            //    double error_best; //= ??????

            //    int iterations = 0;
            //    double[] error = new double[k];

            //    // Until k iterations have occurrec
            //    while (iterations < k)
            //    {
            //        int ii = 0;

            //        #region CLEAN THIS

            //        //clear p_close dist p_new p_in p_out

            //        // Draw a sample of n points from the data
            //        int perm=rand.Next(p.Length);
            //        int sample_in=rand.Next(1,no);
            //        Point3D p_in=p[sample_in];
            //        int sample_out=perm(no+1:end);
            //        //p_out=p(sample_out,:);        // why pull this if it's never used???

            //        //// Fit to that set of n points
            //        //[n_est_in ro_est_in]=LSE(p_in);

            //        //// For each data point oustide the sample
            //        //for i=sample_out
            //        //    dist=dot(n_est_in,p(i,:))-ro_est_in;
            //        //    // Test distance d to t
            //        //    abs(dist);
            //        //    if abs(dist)<t // If d<t, the point is close
            //        //        ii=ii+1;
            //        //        p_close(ii,:)=p(i,:);
            //        //    end
            //        //end

            //        //p_new=[p_in;p_close];

            //        //// If there are d or more points close to the line
            //        //if length(p_new) > d
            //        //    // Refit the line using all these points
            //        //    [n_est_new ro_est_new X Y Z]=LSE(p_new);
            //        //    for iii=1:length(p_new)
            //        //        dist(iii)=dot(n_est_new,p_new(iii,:))-ro_est_new;
            //        //    end
            //        //    // Use the fitting error as error criterion (ive used SAD for ease)
            //        //    error[iterations+1]=sum(abs(dist));
            //        //else
            //        //    error[iterations+1]=inf;
            //        //end

            //        #endregion


            //        if (iterations > 1)
            //        {
            //                // Use the best fit from this collection 
            //            if (error[iterations + 1] <= error.Min())
            //            {
            //                //        p_best=p_new;
            //                //        n_best=n_est_new;
            //                //        ro_best=ro_est_new;
            //                //        X_best=X;
            //                //        Y_best=Y;
            //                //        Z_best=Z;
            //                //        error_best=error[iterations+1];
            //            }
            //        }

            //        iterations = iterations + 1;
            //    }


            //}

            #endregion


            //http://codesuppository.blogspot.com/2006/03/best-fit-plane.html
            //http://codesuppository.blogspot.com/2009/06/holy-crap-my-veyr-own-charles.html
            //https://code.google.com/p/codesuppository/source/browse/trunk/app/CodeSuppository/
            //http://www.geometrictools.com/
            #region Orig C

            //#include <string.h>
            //#include <stdio.h>
            //#include <stdlib.h>
            //#include <math.h>
            //#include <float.h>

            //typedef unsigned int NxU32;
            //typedef int NxI32;
            //typedef float REAL;

            //const REAL BF_PI = 3.1415926535897932384626433832795028841971693993751f;
            //const REAL BF_DEG_TO_RAD = ((2.0f * BF_PI) / 360.0f);
            //const REAL BF_RAD_TO_DEG = (360.0f / (2.0f * BF_PI));

            //void bf_getTranslation(const REAL *matrix,REAL *t)
            //{
            //  t[0] = matrix[3*4+0];
            //  t[1] = matrix[3*4+1];
            //  t[2] = matrix[3*4+2];
            //}

            //void bf_matrixToQuat(const REAL *matrix,REAL *quat) // convert the 3x3 portion of a 4x4 matrix into a quaterion as x,y,z,w
            //{

            //  REAL tr = matrix[0*4+0] + matrix[1*4+1] + matrix[2*4+2];

            //  // check the diagonal

            //  if (tr > 0.0f )
            //  {
            //    REAL s = (REAL) sqrt ( (double) (tr + 1.0f) );
            //    quat[3] = s * 0.5f;
            //    s = 0.5f / s;
            //    quat[0] = (matrix[1*4+2] - matrix[2*4+1]) * s;
            //    quat[1] = (matrix[2*4+0] - matrix[0*4+2]) * s;
            //    quat[2] = (matrix[0*4+1] - matrix[1*4+0]) * s;

            //  }
            //  else
            //  {
            //    // diagonal is negative
            //    NxI32 nxt[3] = {1, 2, 0};
            //    REAL  qa[4];

            //    NxI32 i = 0;

            //    if (matrix[1*4+1] > matrix[0*4+0]) i = 1;
            //    if (matrix[2*4+2] > matrix[i*4+i]) i = 2;

            //    NxI32 j = nxt[i];
            //    NxI32 k = nxt[j];

            //    REAL s = sqrt ( ((matrix[i*4+i] - (matrix[j*4+j] + matrix[k*4+k])) + 1.0f) );

            //    qa[i] = s * 0.5f;

            //    if (s != 0.0f ) s = 0.5f / s;

            //    qa[3] = (matrix[j*4+k] - matrix[k*4+j]) * s;
            //    qa[j] = (matrix[i*4+j] + matrix[j*4+i]) * s;
            //    qa[k] = (matrix[i*4+k] + matrix[k*4+i]) * s;

            //    quat[0] = qa[0];
            //    quat[1] = qa[1];
            //    quat[2] = qa[2];
            //    quat[3] = qa[3];
            //  }


            //}



            //void  bf_matrixMultiply(const REAL *pA,const REAL *pB,REAL *pM)
            //{
            //  REAL a = pA[0*4+0] * pB[0*4+0] + pA[0*4+1] * pB[1*4+0] + pA[0*4+2] * pB[2*4+0] + pA[0*4+3] * pB[3*4+0];
            //  REAL b = pA[0*4+0] * pB[0*4+1] + pA[0*4+1] * pB[1*4+1] + pA[0*4+2] * pB[2*4+1] + pA[0*4+3] * pB[3*4+1];
            //  REAL c = pA[0*4+0] * pB[0*4+2] + pA[0*4+1] * pB[1*4+2] + pA[0*4+2] * pB[2*4+2] + pA[0*4+3] * pB[3*4+2];
            //  REAL d = pA[0*4+0] * pB[0*4+3] + pA[0*4+1] * pB[1*4+3] + pA[0*4+2] * pB[2*4+3] + pA[0*4+3] * pB[3*4+3];

            //  REAL e = pA[1*4+0] * pB[0*4+0] + pA[1*4+1] * pB[1*4+0] + pA[1*4+2] * pB[2*4+0] + pA[1*4+3] * pB[3*4+0];
            //  REAL f = pA[1*4+0] * pB[0*4+1] + pA[1*4+1] * pB[1*4+1] + pA[1*4+2] * pB[2*4+1] + pA[1*4+3] * pB[3*4+1];
            //  REAL g = pA[1*4+0] * pB[0*4+2] + pA[1*4+1] * pB[1*4+2] + pA[1*4+2] * pB[2*4+2] + pA[1*4+3] * pB[3*4+2];
            //  REAL h = pA[1*4+0] * pB[0*4+3] + pA[1*4+1] * pB[1*4+3] + pA[1*4+2] * pB[2*4+3] + pA[1*4+3] * pB[3*4+3];

            //  REAL i = pA[2*4+0] * pB[0*4+0] + pA[2*4+1] * pB[1*4+0] + pA[2*4+2] * pB[2*4+0] + pA[2*4+3] * pB[3*4+0];
            //  REAL j = pA[2*4+0] * pB[0*4+1] + pA[2*4+1] * pB[1*4+1] + pA[2*4+2] * pB[2*4+1] + pA[2*4+3] * pB[3*4+1];
            //  REAL k = pA[2*4+0] * pB[0*4+2] + pA[2*4+1] * pB[1*4+2] + pA[2*4+2] * pB[2*4+2] + pA[2*4+3] * pB[3*4+2];
            //  REAL l = pA[2*4+0] * pB[0*4+3] + pA[2*4+1] * pB[1*4+3] + pA[2*4+2] * pB[2*4+3] + pA[2*4+3] * pB[3*4+3];

            //  REAL m = pA[3*4+0] * pB[0*4+0] + pA[3*4+1] * pB[1*4+0] + pA[3*4+2] * pB[2*4+0] + pA[3*4+3] * pB[3*4+0];
            //  REAL n = pA[3*4+0] * pB[0*4+1] + pA[3*4+1] * pB[1*4+1] + pA[3*4+2] * pB[2*4+1] + pA[3*4+3] * pB[3*4+1];
            //  REAL o = pA[3*4+0] * pB[0*4+2] + pA[3*4+1] * pB[1*4+2] + pA[3*4+2] * pB[2*4+2] + pA[3*4+3] * pB[3*4+2];
            //  REAL p = pA[3*4+0] * pB[0*4+3] + pA[3*4+1] * pB[1*4+3] + pA[3*4+2] * pB[2*4+3] + pA[3*4+3] * pB[3*4+3];

            //  pM[0] = a;
            //  pM[1] = b;
            //  pM[2] = c;
            //  pM[3] = d;

            //  pM[4] = e;
            //  pM[5] = f;
            //  pM[6] = g;
            //  pM[7] = h;

            //  pM[8] = i;
            //  pM[9] = j;
            //  pM[10] = k;
            //  pM[11] = l;

            //  pM[12] = m;
            //  pM[13] = n;
            //  pM[14] = o;
            //  pM[15] = p;
            //}




            //void bf_eulerToQuat(REAL roll,REAL pitch,REAL yaw,REAL *quat) // convert euler angles to quaternion.
            //{
            //  roll  *= 0.5f;
            //  pitch *= 0.5f;
            //  yaw   *= 0.5f;

            //  REAL cr = cos(roll);
            //  REAL cp = cos(pitch);
            //  REAL cy = cos(yaw);

            //  REAL sr = sin(roll);
            //  REAL sp = sin(pitch);
            //  REAL sy = sin(yaw);

            //  REAL cpcy = cp * cy;
            //  REAL spsy = sp * sy;
            //  REAL spcy = sp * cy;
            //  REAL cpsy = cp * sy;

            //  quat[0]   = ( sr * cpcy - cr * spsy);
            //  quat[1]   = ( cr * spcy + sr * cpsy);
            //  quat[2]   = ( cr * cpsy - sr * spcy);
            //  quat[3]   = cr * cpcy + sr * spsy;
            //}

            //void  bf_eulerToQuat(const REAL *euler,REAL *quat) // convert euler angles to quaternion.
            //{
            //  bf_eulerToQuat(euler[0],euler[1],euler[2],quat);
            //}



            //void  bf_setTranslation(const REAL *translation,REAL *matrix)
            //{
            //  matrix[12] = translation[0];
            //  matrix[13] = translation[1];
            //  matrix[14] = translation[2];
            //}


            //void  bf_transform(const REAL matrix[16],const REAL v[3],REAL t[3]) // rotate and translate this point
            //{
            //  if ( matrix )
            //  {
            //    REAL tx = (matrix[0*4+0] * v[0]) +  (matrix[1*4+0] * v[1]) + (matrix[2*4+0] * v[2]) + matrix[3*4+0];
            //    REAL ty = (matrix[0*4+1] * v[0]) +  (matrix[1*4+1] * v[1]) + (matrix[2*4+1] * v[2]) + matrix[3*4+1];
            //    REAL tz = (matrix[0*4+2] * v[0]) +  (matrix[1*4+2] * v[1]) + (matrix[2*4+2] * v[2]) + matrix[3*4+2];
            //    t[0] = tx;
            //    t[1] = ty;
            //    t[2] = tz;
            //  }
            //  else
            //  {
            //    t[0] = v[0];
            //    t[1] = v[1];
            //    t[2] = v[2];
            //  }
            //}



            //REAL bf_dot(const REAL *p1,const REAL *p2)
            //{
            //  return p1[0]*p2[0]+p1[1]*p2[1]+p1[2]*p2[2];
            //}

            //void bf_cross(REAL *cross,const REAL *a,const REAL *b)
            //{
            //  cross[0] = a[1]*b[2] - a[2]*b[1];
            //  cross[1] = a[2]*b[0] - a[0]*b[2];
            //  cross[2] = a[0]*b[1] - a[1]*b[0];
            //}


            //void bf_quatToMatrix(const REAL *quat,REAL *matrix) // convert quaterinion rotation to matrix, zeros out the translation component.
            //{

            //  REAL xx = quat[0]*quat[0];
            //  REAL yy = quat[1]*quat[1];
            //  REAL zz = quat[2]*quat[2];
            //  REAL xy = quat[0]*quat[1];
            //  REAL xz = quat[0]*quat[2];
            //  REAL yz = quat[1]*quat[2];
            //  REAL wx = quat[3]*quat[0];
            //  REAL wy = quat[3]*quat[1];
            //  REAL wz = quat[3]*quat[2];

            //  matrix[0*4+0] = 1 - 2 * ( yy + zz );
            //  matrix[1*4+0] =     2 * ( xy - wz );
            //  matrix[2*4+0] =     2 * ( xz + wy );

            //  matrix[0*4+1] =     2 * ( xy + wz );
            //  matrix[1*4+1] = 1 - 2 * ( xx + zz );
            //  matrix[2*4+1] =     2 * ( yz - wx );

            //  matrix[0*4+2] =     2 * ( xz - wy );
            //  matrix[1*4+2] =     2 * ( yz + wx );
            //  matrix[2*4+2] = 1 - 2 * ( xx + yy );

            //  matrix[3*4+0] = matrix[3*4+1] = matrix[3*4+2] = (REAL) 0.0f;
            //  matrix[0*4+3] = matrix[1*4+3] = matrix[2*4+3] = (REAL) 0.0f;
            //  matrix[3*4+3] =(REAL) 1.0f;

            //}



            //// Reference, from Stan Melax in Game Gems I
            ////  Quaternion q;
            ////  vector3 c = CrossProduct(v0,v1);
            ////  REAL   d = DotProduct(v0,v1);
            ////  REAL   s = (REAL)sqrt((1+d)*2);
            ////  q.x = c.x / s;
            ////  q.y = c.y / s;
            ////  q.z = c.z / s;
            ////  q.w = s /2.0f;
            ////  return q;
            //void bf_rotationArc(const REAL *v0,const REAL *v1,REAL *quat)
            //{
            //  REAL cross[3];

            //  bf_cross(cross,v0,v1);
            //  REAL d = bf_dot(v0,v1);
            //  REAL s = sqrt((1+d)*2);
            //  REAL recip = 1.0f / s;

            //  quat[0] = cross[0] * recip;
            //  quat[1] = cross[1] * recip;
            //  quat[2] = cross[2] * recip;
            //  quat[3] = s * 0.5f;

            //}


            //void bf_planeToMatrix(const REAL *plane,REAL *matrix) // convert a plane equation to a 4x4 rotation matrix
            //{
            //  REAL ref[3] = { 0, 1, 0 };
            //  REAL quat[4];
            //  bf_rotationArc(ref,plane,quat);
            //  bf_quatToMatrix(quat,matrix);
            //  REAL origin[3] = { 0, -plane[3], 0 };
            //  REAL center[3];
            //  bf_transform(matrix,origin,center);
            //  bf_setTranslation(center,matrix);
            //}



            //void bf_inverseRT(const REAL matrix[16],const REAL pos[3],REAL t[3]) // inverse rotate translate the point.
            //{

            //        REAL _x = pos[0] - matrix[3*4+0];
            //        REAL _y = pos[1] - matrix[3*4+1];
            //        REAL _z = pos[2] - matrix[3*4+2];

            //        // Multiply inverse-translated source vector by inverted rotation transform

            //        t[0] = (matrix[0*4+0] * _x) + (matrix[0*4+1] * _y) + (matrix[0*4+2] * _z);
            //        t[1] = (matrix[1*4+0] * _x) + (matrix[1*4+1] * _y) + (matrix[1*4+2] * _z);
            //        t[2] = (matrix[2*4+0] * _x) + (matrix[2*4+1] * _y) + (matrix[2*4+2] * _z);

            //}

            //void  bf_rotate(const REAL matrix[16],const REAL v[3],REAL t[3]) // rotate and translate this point
            //{
            //  if ( matrix )
            //  {
            //    REAL tx = (matrix[0*4+0] * v[0]) +  (matrix[1*4+0] * v[1]) + (matrix[2*4+0] * v[2]);
            //    REAL ty = (matrix[0*4+1] * v[0]) +  (matrix[1*4+1] * v[1]) + (matrix[2*4+1] * v[2]);
            //    REAL tz = (matrix[0*4+2] * v[0]) +  (matrix[1*4+2] * v[1]) + (matrix[2*4+2] * v[2]);
            //    t[0] = tx;
            //    t[1] = ty;
            //    t[2] = tz;
            //  }
            //  else
            //  {
            //    t[0] = v[0];
            //    t[1] = v[1];
            //    t[2] = v[2];
            //  }
            //}


            //// computes the OBB for this set of points relative to this transform matrix.
            //void bf_computeOBB(NxU32 vcount,const REAL *points,NxU32 pstride,REAL *sides,REAL *matrix)
            //{
            //  const char *src = (const char *) points;

            //  REAL bmin[3] = { 1e9, 1e9, 1e9 };
            //  REAL bmax[3] = { -1e9, -1e9, -1e9 };

            //  for (NxU32 i=0; i<vcount; i++)
            //  {
            //    const REAL *p = (const REAL *) src;
            //    REAL t[3];

            //    bf_inverseRT(matrix, p, t ); // inverse rotate translate

            //    if ( t[0] < bmin[0] ) bmin[0] = t[0];
            //    if ( t[1] < bmin[1] ) bmin[1] = t[1];
            //    if ( t[2] < bmin[2] ) bmin[2] = t[2];

            //    if ( t[0] > bmax[0] ) bmax[0] = t[0];
            //    if ( t[1] > bmax[1] ) bmax[1] = t[1];
            //    if ( t[2] > bmax[2] ) bmax[2] = t[2];

            //    src+=pstride;
            //  }

            //  REAL center[3];

            //  sides[0] = bmax[0]-bmin[0];
            //  sides[1] = bmax[1]-bmin[1];
            //  sides[2] = bmax[2]-bmin[2];

            //  center[0] = sides[0]*0.5f+bmin[0];
            //  center[1] = sides[1]*0.5f+bmin[1];
            //  center[2] = sides[2]*0.5f+bmin[2];

            //  REAL ocenter[3];

            //  bf_rotate(matrix,center,ocenter);

            //  matrix[12]+=ocenter[0];
            //  matrix[13]+=ocenter[1];
            //  matrix[14]+=ocenter[2];

            //}

            //template <class Type> class Eigen
            //{
            //public:
            //  void DecrSortEigenStuff(void)
            //  {
            //    Tridiagonal(); //diagonalize the matrix.
            //    QLAlgorithm(); //
            //    DecreasingSort();
            //    GuaranteeRotation();
            //  }

            //  void Tridiagonal(void)
            //  {
            //    Type fM00 = mElement[0][0];
            //    Type fM01 = mElement[0][1];
            //    Type fM02 = mElement[0][2];
            //    Type fM11 = mElement[1][1];
            //    Type fM12 = mElement[1][2];
            //    Type fM22 = mElement[2][2];

            //    m_afDiag[0] = fM00;
            //    m_afSubd[2] = 0;
            //    if (fM02 != (Type)0.0)
            //    {
            //      Type fLength = sqrt(fM01*fM01+fM02*fM02);
            //      Type fInvLength = ((Type)1.0)/fLength;
            //      fM01 *= fInvLength;
            //      fM02 *= fInvLength;
            //      Type fQ = ((Type)2.0)*fM01*fM12+fM02*(fM22-fM11);
            //      m_afDiag[1] = fM11+fM02*fQ;
            //      m_afDiag[2] = fM22-fM02*fQ;
            //      m_afSubd[0] = fLength;
            //      m_afSubd[1] = fM12-fM01*fQ;
            //      mElement[0][0] = (Type)1.0;
            //      mElement[0][1] = (Type)0.0;
            //      mElement[0][2] = (Type)0.0;
            //      mElement[1][0] = (Type)0.0;
            //      mElement[1][1] = fM01;
            //      mElement[1][2] = fM02;
            //      mElement[2][0] = (Type)0.0;
            //      mElement[2][1] = fM02;
            //      mElement[2][2] = -fM01;
            //      m_bIsRotation = false;
            //    }
            //    else
            //    {
            //      m_afDiag[1] = fM11;
            //      m_afDiag[2] = fM22;
            //      m_afSubd[0] = fM01;
            //      m_afSubd[1] = fM12;
            //      mElement[0][0] = (Type)1.0;
            //      mElement[0][1] = (Type)0.0;
            //      mElement[0][2] = (Type)0.0;
            //      mElement[1][0] = (Type)0.0;
            //      mElement[1][1] = (Type)1.0;
            //      mElement[1][2] = (Type)0.0;
            //      mElement[2][0] = (Type)0.0;
            //      mElement[2][1] = (Type)0.0;
            //      mElement[2][2] = (Type)1.0;
            //      m_bIsRotation = true;
            //    }
            //  }

            //  bool QLAlgorithm(void)
            //  {
            //    const NxI32 iMaxIter = 32;

            //    for (NxI32 i0 = 0; i0 <3; i0++)
            //    {
            //      NxI32 i1;
            //      for (i1 = 0; i1 < iMaxIter; i1++)
            //      {
            //        NxI32 i2;
            //        for (i2 = i0; i2 <= (3-2); i2++)
            //        {
            //          Type fTmp = fabs(m_afDiag[i2]) + fabs(m_afDiag[i2+1]);
            //          if ( fabs(m_afSubd[i2]) + fTmp == fTmp )
            //            break;
            //        }
            //        if (i2 == i0)
            //        {
            //          break;
            //        }

            //        Type fG = (m_afDiag[i0+1] - m_afDiag[i0])/(((Type)2.0) * m_afSubd[i0]);
            //        Type fR = sqrt(fG*fG+(Type)1.0);
            //        if (fG < (Type)0.0)
            //        {
            //          fG = m_afDiag[i2]-m_afDiag[i0]+m_afSubd[i0]/(fG-fR);
            //        }
            //        else
            //        {
            //          fG = m_afDiag[i2]-m_afDiag[i0]+m_afSubd[i0]/(fG+fR);
            //        }
            //        Type fSin = (Type)1.0, fCos = (Type)1.0, fP = (Type)0.0;
            //        for (NxI32 i3 = i2-1; i3 >= i0; i3--)
            //        {
            //          Type fF = fSin*m_afSubd[i3];
            //          Type fB = fCos*m_afSubd[i3];
            //          if (fabs(fF) >= fabs(fG))
            //          {
            //            fCos = fG/fF;
            //            fR = sqrt(fCos*fCos+(Type)1.0);
            //            m_afSubd[i3+1] = fF*fR;
            //            fSin = ((Type)1.0)/fR;
            //            fCos *= fSin;
            //          }
            //          else
            //          {
            //            fSin = fF/fG;
            //            fR = sqrt(fSin*fSin+(Type)1.0);
            //            m_afSubd[i3+1] = fG*fR;
            //            fCos = ((Type)1.0)/fR;
            //            fSin *= fCos;
            //          }
            //          fG = m_afDiag[i3+1]-fP;
            //          fR = (m_afDiag[i3]-fG)*fSin+((Type)2.0)*fB*fCos;
            //          fP = fSin*fR;
            //          m_afDiag[i3+1] = fG+fP;
            //          fG = fCos*fR-fB;
            //          for (NxI32 i4 = 0; i4 < 3; i4++)
            //          {
            //            fF = mElement[i4][i3+1];
            //            mElement[i4][i3+1] = fSin*mElement[i4][i3]+fCos*fF;
            //            mElement[i4][i3] = fCos*mElement[i4][i3]-fSin*fF;
            //          }
            //        }
            //        m_afDiag[i0] -= fP;
            //        m_afSubd[i0] = fG;
            //        m_afSubd[i2] = (Type)0.0;
            //      }
            //      if (i1 == iMaxIter)
            //      {
            //        return false;
            //      }
            //    }
            //    return true;
            //  }

            //  void DecreasingSort(void)
            //  {
            //    //sort eigenvalues in decreasing order, e[0] >= ... >= e[iSize-1]
            //    for (NxI32 i0 = 0, i1; i0 <= 3-2; i0++)
            //    {
            //      // locate maximum eigenvalue
            //      i1 = i0;
            //      Type fMax = m_afDiag[i1];
            //      NxI32 i2;
            //      for (i2 = i0+1; i2 < 3; i2++)
            //      {
            //        if (m_afDiag[i2] > fMax)
            //        {
            //          i1 = i2;
            //          fMax = m_afDiag[i1];
            //        }
            //      }

            //      if (i1 != i0)
            //      {
            //        // swap eigenvalues
            //        m_afDiag[i1] = m_afDiag[i0];
            //        m_afDiag[i0] = fMax;
            //        // swap eigenvectors
            //        for (i2 = 0; i2 < 3; i2++)
            //        {
            //          Type fTmp = mElement[i2][i0];
            //          mElement[i2][i0] = mElement[i2][i1];
            //          mElement[i2][i1] = fTmp;
            //          m_bIsRotation = !m_bIsRotation;
            //        }
            //      }
            //    }
            //  }


            //  void GuaranteeRotation(void)
            //  {
            //    if (!m_bIsRotation)
            //    {
            //      // change sign on the first column
            //      for (NxI32 iRow = 0; iRow <3; iRow++)
            //      {
            //        mElement[iRow][0] = -mElement[iRow][0];
            //      }
            //    }
            //  }

            //  Type mElement[3][3];
            //  Type m_afDiag[3];
            //  Type m_afSubd[3];
            //  bool m_bIsRotation;
            //};

            //bool bf_computeBestFitPlane(NxU32 vcount,
            //                     const REAL *points,
            //                     NxU32 vstride,
            //                     const REAL *weights,
            //                     NxU32 wstride,
            //                     REAL *plane)
            //{
            //  bool ret = false;

            //  REAL kOrigin[3] = { 0, 0, 0 };

            //  REAL wtotal = 0;

            //  {
            //    const char *source  = (const char *) points;
            //    const char *wsource = (const char *) weights;

            //    for (NxU32 i=0; i<vcount; i++)
            //    {

            //      const REAL *p = (const REAL *) source;

            //      REAL w = 1;

            //      if ( wsource )
            //      {
            //        const REAL *ws = (const REAL *) wsource;
            //        w = *ws; //
            //        wsource+=wstride;
            //      }

            //      kOrigin[0]+=p[0]*w;
            //      kOrigin[1]+=p[1]*w;
            //      kOrigin[2]+=p[2]*w;

            //      wtotal+=w;

            //      source+=vstride;
            //    }
            //  }

            //  REAL recip = 1.0f / wtotal; // reciprocol of total weighting

            //  kOrigin[0]*=recip;
            //  kOrigin[1]*=recip;
            //  kOrigin[2]*=recip;


            //  REAL fSumXX=0;
            //  REAL fSumXY=0;
            //  REAL fSumXZ=0;

            //  REAL fSumYY=0;
            //  REAL fSumYZ=0;
            //  REAL fSumZZ=0;


            //  {
            //    const char *source  = (const char *) points;
            //    const char *wsource = (const char *) weights;

            //    for (NxU32 i=0; i<vcount; i++)
            //    {

            //      const REAL *p = (const REAL *) source;

            //      REAL w = 1;

            //      if ( wsource )
            //      {
            //        const REAL *ws = (const REAL *) wsource;
            //        w = *ws; //
            //        wsource+=wstride;
            //      }

            //      REAL kDiff[3];

            //      kDiff[0] = w*(p[0] - kOrigin[0]); // apply vertex weighting!
            //      kDiff[1] = w*(p[1] - kOrigin[1]);
            //      kDiff[2] = w*(p[2] - kOrigin[2]);

            //      fSumXX+= kDiff[0] * kDiff[0]; // sume of the squares of the differences.
            //      fSumXY+= kDiff[0] * kDiff[1]; // sume of the squares of the differences.
            //      fSumXZ+= kDiff[0] * kDiff[2]; // sume of the squares of the differences.

            //      fSumYY+= kDiff[1] * kDiff[1];
            //      fSumYZ+= kDiff[1] * kDiff[2];
            //      fSumZZ+= kDiff[2] * kDiff[2];


            //      source+=vstride;
            //    }
            //  }

            //  fSumXX *= recip;
            //  fSumXY *= recip;
            //  fSumXZ *= recip;
            //  fSumYY *= recip;
            //  fSumYZ *= recip;
            //  fSumZZ *= recip;

            //  // setup the eigensolver
            //  Eigen<REAL> kES;

            //  kES.mElement[0][0] = fSumXX;
            //  kES.mElement[0][1] = fSumXY;
            //  kES.mElement[0][2] = fSumXZ;

            //  kES.mElement[1][0] = fSumXY;
            //  kES.mElement[1][1] = fSumYY;
            //  kES.mElement[1][2] = fSumYZ;

            //  kES.mElement[2][0] = fSumXZ;
            //  kES.mElement[2][1] = fSumYZ;
            //  kES.mElement[2][2] = fSumZZ;

            //  // compute eigenstuff, smallest eigenvalue is in last position
            //  kES.DecrSortEigenStuff();

            //  REAL kNormal[3];

            //  kNormal[0] = kES.mElement[0][2];
            //  kNormal[1] = kES.mElement[1][2];
            //  kNormal[2] = kES.mElement[2][2];

            //  // the minimum energy
            //  plane[0] = kNormal[0];
            //  plane[1] = kNormal[1];
            //  plane[2] = kNormal[2];

            //  plane[3] = 0 - bf_dot(kNormal,kOrigin);

            //  ret = true;

            //  return ret;
            //}



            //void bf_computeBestFitOBB(NxU32 vcount,const REAL *points,NxU32 pstride,REAL *sides,REAL *matrix,bool bruteForce)
            //{
            //  REAL plane[4];
            //  bf_computeBestFitPlane(vcount,points,pstride,0,0,plane);
            //  bf_planeToMatrix(plane,matrix);
            //  bf_computeOBB( vcount, points, pstride, sides, matrix );

            //  REAL refmatrix[16];
            //  memcpy(refmatrix,matrix,16*sizeof(REAL));

            //  REAL volume = sides[0]*sides[1]*sides[2];
            //  if ( bruteForce )
            //  {
            //    for (REAL a=10; a<180; a+=10)
            //    {
            //      REAL quat[4];
            //      bf_eulerToQuat(0,a*BF_DEG_TO_RAD,0,quat);
            //      REAL temp[16];
            //      REAL pmatrix[16];
            //      bf_quatToMatrix(quat,temp);
            //      bf_matrixMultiply(temp,refmatrix,pmatrix);
            //      REAL psides[3];
            //      bf_computeOBB( vcount, points, pstride, psides, pmatrix );
            //      REAL v = psides[0]*psides[1]*psides[2];
            //      if ( v < volume )
            //      {
            //        volume = v;
            //        memcpy(matrix,pmatrix,sizeof(REAL)*16);
            //        sides[0] = psides[0];
            //        sides[1] = psides[1];
            //        sides[2] = psides[2];
            //      }
            //    }
            //  }
            //}

            //void bf_computeBestFitOBB(NxU32 vcount,const REAL *points,NxU32 pstride,REAL *sides,REAL *pos,REAL *quat,bool bruteForce)
            //{
            //  REAL matrix[16];
            //  bf_computeBestFitOBB(vcount,points,pstride,sides,matrix,bruteForce);
            //  bf_getTranslation(matrix,pos);
            //  bf_matrixToQuat(matrix,quat);
            //}


            //#define TEST_MAIN 1

            //#if TEST_MAIN


            //static REAL points[] = {55.310600f, 217.810000f, 141.659000f,
            //179.067000f, 168.147000f, 228.722000f,  113.104000f, 73.912100f, 179.256000f,  71.613400f, 110.200000f, 137.975000f,  212.179000f, 151.760000f, 216.908000f,
            //47.166000f, 151.760000f, 96.693900f,  179.154000f, 72.552700f, 204.025000f,  162.637000f, 201.294000f, 171.002000f,  150.726000f, 209.554000f, 179.256000f,
            //146.129000f, 73.006900f, 195.769000f,  51.075400f, 143.504000f, 146.231000f,  195.667000f, 89.762000f, 228.794000f,  220.251000f, 109.784000f, 212.608000f,
            //129.617000f, 135.248000f, 151.976000f,  179.154000f, 160.016000f, 234.533000f,  80.079300f, 258.404000f, 186.495000f,  129.617000f, 75.683200f, 137.975000f,
            //162.642000f, 184.785000f, 148.828000f,  154.385000f, 105.632000f, 237.050000f,  214.520000f, 102.223000f, 195.769000f,  62.664900f, 184.785000f, 220.537000f,
            //162.642000f, 66.899100f, 187.512000f,  96.591800f, 209.554000f, 227.138000f,  47.054400f, 160.016000f, 193.835000f,  137.873000f, 66.567700f, 162.744000f,
            //203.923000f, 61.829800f, 154.488000f,  137.873000f, 135.248000f, 248.469000f,  187.410000f, 110.479000f, 241.251000f,  195.667000f, 135.248000f, 184.418000f,
            //88.335600f, 140.811000f, 245.306000f,  220.435000f, 110.479000f, 237.522000f,  228.692000f, 70.842000f, 204.025000f,  203.887000f, 110.501000f, 237.047000f,
            //136.389000f, 168.273000f, 129.719000f,  203.923000f, 77.454000f, 216.468000f,  222.831000f, 143.504000f, 228.794000f,  146.129000f, 151.760000f, 246.179000f,
            //104.848000f, 176.529000f, 135.517000f,  184.034000f, 93.966500f, 154.488000f,  137.873000f, 209.554000f, 167.375000f,  183.735000f, 160.016000f, 195.769000f,
            //104.848000f, 217.810000f, 223.446000f,  101.477000f, 135.248000f, 146.231000f,  90.820200f, 176.529000f, 129.719000f,  88.335600f, 110.479000f, 239.023000f,
            //104.848000f, 201.298000f, 230.467000f,  80.079300f, 97.167100f, 171.000000f,  228.692000f, 91.308600f, 212.281000f,  190.517000f, 102.223000f, 162.744000f,
            //96.591800f, 205.273000f, 228.794000f,  47.054400f, 217.810000f, 188.829000f,  96.591800f, 193.041000f, 232.342000f,  75.540400f, 176.529000f, 121.463000f,
            //228.692000f, 97.335100f, 237.050000f,  146.129000f, 85.710300f, 217.503000f,  50.727500f, 143.504000f, 137.975000f,  187.410000f, 118.735000f, 239.813000f,
            //187.410000f, 102.223000f, 160.954000f,  113.104000f, 135.248000f, 252.430000f,  48.405700f, 160.016000f, 96.693900f,  48.319900f, 151.760000f, 113.206000f,
            //121.361000f, 118.735000f, 142.193000f,  129.617000f, 110.479000f, 142.609000f,  54.412300f, 176.672000f, 104.775000f,  56.279700f, 143.504000f, 212.281000f,
            //166.582000f, 193.041000f, 220.537000f,  55.143400f, 160.016000f, 212.651000f,  170.898000f, 151.760000f, 163.266000f,  86.817200f, 118.735000f, 137.975000f,
            //41.493300f, 184.785000f, 187.512000f,  179.154000f, 126.991000f, 239.541000f,  140.231000f, 201.298000f, 146.231000f,  121.361000f, 184.785000f, 238.332000f,
            //72.771200f, 110.479000f, 220.537000f,  146.129000f, 203.866000f, 220.537000f,  96.591800f, 250.835000f, 156.360000f,  113.104000f, 143.504000f, 252.036000f,
            //141.567000f, 201.298000f, 162.744000f,  63.566900f, 184.785000f, 119.288000f,  104.848000f, 149.174000f, 146.231000f,  104.848000f, 168.273000f, 243.591000f,
            //168.991000f, 168.273000f, 162.744000f,  38.798200f, 176.529000f, 155.049000f,  187.410000f, 143.504000f, 177.567000f,  233.032000f, 102.223000f, 228.794000f,
            //74.211300f, 184.785000f, 228.794000f,  154.385000f, 193.041000f, 226.621000f,  212.179000f, 161.896000f, 228.794000f,  55.555600f, 135.752000f, 162.744000f,
            //46.966300f, 193.230000f, 129.519000f,  54.669400f, 135.248000f, 179.256000f,  220.435000f, 93.548100f, 195.769000f,  203.923000f, 151.760000f, 236.130000f,
            //96.591800f, 89.728000f, 129.719000f,  212.179000f, 102.223000f, 236.223000f,  63.566900f, 122.141000f, 121.463000f,  121.361000f, 135.248000f, 251.009000f,
            //146.129000f, 135.248000f, 157.772000f,  80.079300f, 258.835000f, 163.506000f,  165.685000f, 102.223000f, 237.050000f,  96.591800f, 151.760000f, 247.220000f,
            //55.236700f, 135.085000f, 196.674000f,  121.361000f, 171.376000f, 129.719000f,  182.036000f, 85.710300f, 146.231000f,  236.948000f, 83.697200f, 204.025000f,
            //146.129000f, 160.016000f, 243.576000f,  228.692000f, 66.096800f, 179.256000f,  75.299500f, 168.273000f, 113.206000f,  79.561700f, 250.946000f, 195.883000f,
            //145.903000f, 217.578000f, 203.620000f,  121.451000f, 250.903000f, 179.048000f,  79.180000f, 135.248000f, 238.088000f,  121.361000f, 176.529000f, 242.627000f,
            //121.598000f, 226.066000f, 212.367000f,  179.154000f, 59.706200f, 179.256000f,  96.591800f, 226.066000f, 160.504000f,  129.617000f, 226.066000f, 166.738000f,
            //80.079300f, 124.140000f, 237.050000f,  153.127000f, 118.357000f, 154.488000f,  63.566900f, 201.298000f, 133.569000f,  54.955300f, 235.070000f, 162.744000f,
            //179.154000f, 101.991000f, 237.311000f,  121.361000f, 246.016000f, 195.769000f,  179.154000f, 118.735000f, 240.240000f,  129.617000f, 151.760000f, 246.540000f,
            //88.335600f, 200.454000f, 146.838000f,  170.898000f, 196.421000f, 212.281000f,  90.573600f, 126.991000f, 137.975000f,  88.401500f, 201.267000f, 228.783000f,
            //96.591800f, 209.554000f, 155.231000f,  104.848000f, 193.041000f, 232.925000f,  216.578000f, 160.016000f, 228.794000f,  40.240800f, 168.273000f, 154.488000f,
            //181.479000f, 160.016000f, 212.281000f,  184.349000f, 143.504000f, 171.000000f,  43.019300f, 168.273000f, 179.256000f,  154.385000f, 68.518600f, 128.837000f,
            //83.204500f, 93.966500f, 212.281000f,  187.410000f, 126.991000f, 238.647000f,  220.496000f, 77.764800f, 219.867000f,  75.160000f, 126.991000f, 113.206000f,
            //137.873000f, 168.273000f, 242.403000f,  137.873000f, 201.298000f, 226.147000f,  113.104000f, 234.322000f, 208.191000f,  64.067700f, 119.452000f, 154.488000f,
            //76.261900f, 151.760000f, 237.050000f,  63.566900f, 168.273000f, 99.889000f,  179.154000f, 77.454000f, 134.003000f,  116.577000f, 110.479000f, 137.975000f,
            //71.823100f, 171.779000f, 113.206000f,  190.916000f, 160.016000f, 220.537000f,  179.154000f, 143.504000f, 165.582000f,  104.848000f, 135.248000f, 252.554000f,
            //104.848000f, 83.338200f, 212.281000f,  68.764100f, 168.273000f, 228.794000f,  220.435000f, 102.223000f, 208.392000f,  80.079300f, 252.338000f, 153.598000f,
            //146.129000f, 184.785000f, 131.625000f,  113.104000f, 74.272800f, 146.231000f,  187.410000f, 56.488000f, 154.488000f,  47.054400f, 221.357000f, 179.256000f,
            //170.898000f, 176.529000f, 230.561000f,  78.999600f, 102.223000f, 137.975000f,  48.648600f, 193.041000f, 204.025000f,  63.566900f, 209.477000f, 138.007000f,
            //129.617000f, 143.504000f, 247.686000f,  220.435000f, 85.710300f, 230.609000f,  44.614300f, 176.529000f, 195.769000f,  220.435000f, 128.098000f, 220.537000f,
            //88.335600f, 184.785000f, 134.975000f,  56.829100f, 234.322000f, 195.769000f,  113.104000f, 113.457000f, 137.975000f,  179.154000f, 151.760000f, 238.587000f,
            //228.573000f, 101.811000f, 220.717000f,  186.520000f, 168.273000f, 195.769000f,  42.084400f, 176.529000f, 137.975000f,  55.310600f, 193.041000f, 124.712000f,
            //88.335600f, 177.607000f, 129.101000f,  121.309000f, 159.997000f, 245.300000f,  170.898000f, 85.710300f, 222.266000f,  195.667000f, 143.504000f, 237.406000f,
            //47.099600f, 151.900000f, 162.744000f,  87.530500f, 135.248000f, 245.306000f,  137.873000f, 88.298700f, 220.537000f,  228.692000f, 67.070000f, 187.512000f,
            //96.591800f, 185.369000f, 137.975000f,  153.202000f, 151.760000f, 245.088000f,  71.823100f, 107.317000f, 154.488000f,  71.823100f, 176.529000f, 118.021000f,
            //121.361000f, 126.991000f, 250.945000f,  88.335600f, 259.848000f, 187.512000f,  162.642000f, 57.939400f, 154.488000f,  88.335600f, 184.785000f, 233.859000f,
            //212.179000f, 135.248000f, 240.508000f,  145.989000f, 69.165400f, 187.611000f,  195.961000f, 60.848500f, 146.018000f,  179.154000f, 57.436000f, 162.744000f,
            //170.898000f, 57.314400f, 154.488000f,  96.591800f, 102.223000f, 127.919000f,  179.154000f, 85.710300f, 143.820000f,  146.129000f, 77.454000f, 121.606000f,
            //154.385000f, 102.223000f, 143.596000f,  236.948000f, 76.857600f, 212.281000f,  174.229000f, 184.785000f, 171.000000f,  129.617000f, 94.953300f, 228.180000f,
            //71.823100f, 257.912000f, 171.000000f,  105.651000f, 201.088000f, 146.435000f,  55.310600f, 151.760000f, 90.261600f,  170.898000f, 85.710300f, 137.049000f,
            //92.876800f, 85.710300f, 154.488000f,  203.923000f, 89.409600f, 162.744000f,  54.458700f, 135.248000f, 129.719000f,  88.335600f, 98.616600f, 228.794000f,
            //146.129000f, 176.529000f, 130.899000f,  55.322000f, 152.091000f, 212.255000f,  195.667000f, 110.479000f, 239.093000f,  121.361000f, 158.723000f, 146.231000f,
            //51.415900f, 151.760000f, 204.025000f,  79.676500f, 193.715000f, 228.794000f,  96.591800f, 118.735000f, 245.933000f,  55.685100f, 136.729000f, 204.025000f,
            //171.011000f, 201.502000f, 178.935000f,  71.823100f, 143.504000f, 99.425000f,  121.361000f, 242.579000f, 198.880000f,  75.525600f, 102.223000f, 212.281000f,
            //104.546000f, 77.306700f, 154.488000f,  84.269500f, 126.991000f, 129.719000f,  96.591800f, 217.810000f, 223.077000f,  129.617000f, 226.066000f, 208.096000f,
            //154.385000f, 77.454000f, 206.123000f,  175.946000f, 193.041000f, 179.256000f,  137.873000f, 216.056000f, 212.281000f,  121.380000f, 94.059200f, 137.967000f,
            //195.667000f, 158.473000f, 228.794000f,  146.129000f, 110.479000f, 238.980000f,  179.282000f, 184.785000f, 178.943000f,  78.566200f, 102.223000f, 220.537000f,
            //129.617000f, 207.039000f, 154.488000f,  71.823100f, 254.988000f, 162.744000f,  88.335600f, 243.988000f, 204.025000f,  212.179000f, 151.133000f, 237.050000f,
            //162.642000f, 183.916000f, 146.231000f,  154.385000f, 110.479000f, 149.019000f,  137.873000f, 207.912000f, 220.537000f,  88.335600f, 88.261300f, 179.256000f,
            //52.810500f, 234.322000f, 187.512000f,  121.361000f, 126.837000f, 146.288000f,  122.345000f, 226.066000f, 162.315000f,  71.859800f, 127.181000f, 228.757000f,
            //80.079300f, 261.720000f, 179.256000f,  179.154000f, 164.456000f, 171.000000f,  121.361000f, 102.223000f, 239.256000f,  55.310600f, 231.470000f, 195.769000f,
            //74.316200f, 160.016000f, 104.950000f,  206.446000f, 126.991000f, 195.769000f,  113.104000f, 126.779000f, 146.436000f,  96.591800f, 117.436000f, 245.306000f,
            //137.873000f, 69.270800f, 178.703000f,  179.154000f, 56.723900f, 154.488000f,  96.591800f, 118.735000f, 143.393000f,  203.923000f, 110.479000f, 178.053000f,
            //96.591800f, 82.015800f, 171.000000f,  154.385000f, 184.785000f, 232.760000f,  195.667000f, 58.401600f, 154.488000f,  129.617000f, 143.504000f, 156.987000f,
            //63.566900f, 187.573000f, 220.537000f,  195.667000f, 139.425000f, 187.512000f,  179.154000f, 69.197800f, 130.998000f,  104.848000f, 250.835000f, 163.866000f,
            //150.903000f, 201.298000f, 220.537000f,  47.054400f, 158.040000f, 187.512000f,  129.617000f, 184.785000f, 237.802000f,  109.270000f, 193.041000f, 137.975000f,
            //45.859800f, 209.554000f, 146.231000f,  154.385000f, 126.991000f, 243.509000f,  187.410000f, 90.789100f, 228.794000f,  68.033200f, 118.735000f, 220.537000f,
            //187.410000f, 66.316700f, 195.769000f,  49.126900f, 226.066000f, 171.000000f,  55.310600f, 234.322000f, 159.875000f,  212.179000f, 79.199600f, 220.537000f,
            //49.980400f, 143.504000f, 113.206000f,  137.873000f, 126.991000f, 248.203000f,  104.848000f, 234.322000f, 210.348000f,  216.982000f, 110.479000f, 237.050000f,
            //80.079300f, 242.579000f, 148.772000f,  96.591800f, 84.231400f, 204.025000f,  93.502800f, 151.760000f, 129.719000f,  96.396300f, 102.157000f, 237.086000f,
            //88.335600f, 226.066000f, 215.538000f,  96.591800f, 88.900600f, 220.537000f,  45.313400f, 168.273000f, 121.463000f,  170.898000f, 206.742000f, 187.512000f,
            //55.310600f, 133.625000f, 187.512000f,  38.798200f, 193.041000f, 156.244000f,  96.591800f, 234.322000f, 210.972000f,  203.923000f, 80.983500f, 154.488000f,
            //121.361000f, 147.639000f, 162.744000f,  212.179000f, 74.886300f, 212.281000f,  162.642000f, 162.835000f, 237.050000f,  91.596500f, 85.710300f, 171.000000f,
            //187.410000f, 69.197800f, 200.446000f,  113.104000f, 102.223000f, 134.964000f,  36.704600f, 193.041000f, 162.744000f,  113.104000f, 234.106000f, 162.813000f,
            //97.462200f, 259.091000f, 162.744000f,  220.435000f, 97.862200f, 204.025000f,  104.848000f, 85.710300f, 132.243000f,  63.452000f, 209.554000f, 212.281000f,
            //80.949600f, 168.273000f, 237.050000f,  49.830400f, 217.810000f, 195.769000f,  71.823100f, 121.521000f, 121.463000f,  212.179000f, 61.826200f, 171.000000f,
            //137.873000f, 226.066000f, 201.434000f,  190.580000f, 118.735000f, 171.000000f,  148.840000f, 217.810000f, 195.769000f,  139.185000f, 201.298000f, 154.488000f,
            //162.629000f, 159.950000f, 162.749000f,  195.667000f, 93.966500f, 232.031000f,  220.435000f, 85.710300f, 175.156000f,  162.642000f, 118.735000f, 242.339000f,
            //212.179000f, 106.372000f, 195.769000f,  228.692000f, 68.956800f, 197.209000f,  207.909000f, 110.479000f, 187.512000f,  79.836600f, 192.936000f, 138.050000f,
            //170.898000f, 77.787600f, 212.281000f,  137.873000f, 160.016000f, 244.607000f,  117.485000f, 176.529000f, 129.719000f,  170.898000f, 110.479000f, 241.297000f,
            //191.688000f, 135.248000f, 179.256000f,  146.129000f, 102.223000f, 234.301000f,  162.642000f, 186.188000f, 228.794000f,  174.168000f, 126.991000f, 162.744000f,
            //129.570000f, 69.189400f, 154.406000f,  94.675400f, 118.735000f, 245.306000f,  95.239000f, 135.248000f, 139.021000f,  223.092000f, 77.454000f, 162.744000f,
            //71.823100f, 151.760000f, 99.707800f,  154.385000f, 193.208000f, 162.405000f,  129.617000f, 157.105000f, 245.306000f,  113.104000f, 81.845800f, 212.281000f,
            //220.435000f, 118.735000f, 217.365000f,  113.104000f, 155.208000f, 154.488000f,  137.282000f, 160.343000f, 146.231000f,  235.175000f, 85.710300f, 228.794000f,
            //228.692000f, 85.710300f, 189.974000f,  162.642000f, 59.402700f, 146.231000f,  203.923000f, 93.966500f, 233.172000f,  162.642000f, 118.735000f, 154.955000f,
            //76.200700f, 102.223000f, 171.000000f,  154.385000f, 118.735000f, 241.955000f,  170.898000f, 160.016000f, 166.333000f,  170.898000f, 184.785000f, 166.531000f,
            //116.121000f, 184.785000f, 129.719000f,  236.948000f, 82.042100f, 212.281000f,  88.335600f, 226.066000f, 157.209000f,  121.409000f, 201.288000f, 137.993000f,
            //170.898000f, 93.569900f, 228.794000f,  104.848000f, 114.341000f, 245.306000f,  88.863000f, 102.223000f, 129.996000f,  119.955000f, 151.760000f, 162.744000f,
            //113.104000f, 201.298000f, 143.291000f,  63.578100f, 250.826000f, 179.256000f,  47.054400f, 212.390000f, 146.231000f,  129.617000f, 238.677000f, 195.769000f,
            //129.617000f, 242.579000f, 182.243000f,  71.823100f, 176.529000f, 229.311000f,  50.981300f, 143.504000f, 179.256000f,  80.079300f, 110.479000f, 135.003000f,
            //162.642000f, 92.434500f, 137.975000f,  80.079300f, 118.735000f, 235.451000f,  212.179000f, 93.966500f, 174.432000f,  63.835800f, 119.396000f, 212.281000f,
            //47.064600f, 209.485000f, 195.737000f,  113.104000f, 163.256000f, 245.306000f,  137.873000f, 203.004000f, 146.231000f,  162.642000f, 209.554000f, 193.218000f,
            //67.805900f, 234.322000f, 146.231000f,  83.613400f, 93.966500f, 146.231000f,  129.617000f, 217.810000f, 165.060000f,  63.566900f, 180.525000f, 113.206000f,
            //74.612200f, 118.735000f, 228.794000f,  137.873000f, 69.197800f, 134.626000f,  228.692000f, 82.328600f, 179.256000f,  121.361000f, 81.106500f, 212.281000f,
            //235.810000f, 77.454000f, 179.256000f,  224.726000f, 110.479000f, 220.537000f,  113.104000f, 93.966500f, 230.239000f,  203.923000f, 143.504000f, 200.456000f,
            //80.079300f, 167.688000f, 236.910000f,  36.365000f, 184.785000f, 162.744000f,  71.823100f, 160.016000f, 102.923000f,  96.591800f, 82.292500f, 187.512000f,
            //202.140000f, 60.941500f, 154.488000f,  68.167200f, 168.273000f, 104.950000f,  96.591800f, 82.057400f, 179.256000f,  113.104000f, 169.095000f, 137.975000f,
            //168.625000f, 176.529000f, 154.488000f,  52.323700f, 209.554000f, 204.025000f,  154.385000f, 212.863000f, 195.769000f,  179.154000f, 57.476900f, 146.231000f,
            //104.848000f, 209.554000f, 227.221000f,  203.923000f, 151.760000f, 208.932000f,  134.861000f, 77.454000f, 129.719000f,  105.766000f, 166.428000f, 137.975000f,
            //74.096300f, 259.091000f, 171.000000f,  203.923000f, 77.454000f, 150.157000f,  131.409000f, 209.554000f, 162.744000f,  96.591800f, 217.810000f, 159.401000f,
            //162.642000f, 102.223000f, 145.102000f,  50.104400f, 226.066000f, 162.744000f,  80.079300f, 132.094000f, 113.206000f,  146.129000f, 201.298000f, 222.972000f,
            //187.410000f, 157.690000f, 212.281000f,  196.734000f, 110.479000f, 170.445000f,  179.377000f, 193.668000f, 186.608000f,  63.566900f, 226.066000f, 143.006000f,
            //236.865000f, 69.226000f, 187.512000f,  104.848000f, 92.224000f, 228.794000f,  71.823100f, 209.554000f, 143.181000f,  74.439600f, 250.835000f, 154.488000f,
            //220.435000f, 91.336500f, 187.512000f,  80.192700f, 242.550000f, 203.999000f,  45.717100f, 217.810000f, 179.256000f,  47.054400f, 201.298000f, 199.142000f,
            //146.129000f, 94.837100f, 227.611000f,  203.863000f, 118.597000f, 187.703000f,  100.395000f, 126.991000f, 146.231000f,  82.252600f, 135.248000f, 113.206000f,
            //134.392000f, 184.785000f, 237.050000f,  63.566900f, 168.273000f, 224.270000f,  170.898000f, 111.667000f, 154.488000f,  196.517000f, 94.647000f, 162.293000f,
            //204.358000f, 60.912100f, 179.487000f,  203.923000f, 70.696900f, 204.025000f,  203.923000f, 154.600000f, 212.281000f,  220.435000f, 93.966500f, 236.053000f,
            //133.726000f, 234.322000f, 195.769000f,  203.923000f, 85.710300f, 159.682000f,  80.079300f, 176.529000f, 233.803000f,  63.566900f, 135.248000f, 222.185000f,
            //45.669200f, 160.016000f, 137.975000f,  48.740400f, 151.760000f, 195.769000f,  159.489000f, 93.966500f, 228.794000f,  96.671000f, 143.122000f, 137.866000f,
            //137.873000f, 72.173800f, 129.719000f,  55.310600f, 224.369000f, 146.231000f,  42.238700f, 176.529000f, 187.512000f,  71.823100f, 116.261000f, 129.719000f,
            //58.885400f, 126.991000f, 121.463000f,  136.876000f, 68.626700f, 137.975000f,  96.591800f, 201.298000f, 148.766000f,  80.079300f, 98.331900f, 154.488000f,
            //96.591800f, 151.760000f, 134.103000f,  67.609100f, 143.504000f, 96.693900f,  162.642000f, 85.491700f, 220.757000f,  146.129000f, 66.577200f, 179.256000f,
            //179.154000f, 196.978000f, 195.769000f,  109.665000f, 259.091000f, 179.256000f,  236.948000f, 81.388500f, 187.512000f,  193.600000f, 85.710300f, 154.488000f,
            //129.617000f, 102.223000f, 140.607000f,  80.079300f, 176.529000f, 124.406000f,  44.258400f, 168.273000f, 187.512000f,  137.873000f, 231.816000f, 187.512000f,
            //111.354000f, 151.760000f, 154.488000f,  173.947000f, 143.504000f, 162.744000f,  212.179000f, 160.016000f, 232.234000f,  187.767000f, 160.016000f, 179.338000f,
            //47.047400f, 159.981000f, 121.463000f,  165.792000f, 151.760000f, 162.744000f,  64.746900f, 119.642000f, 129.719000f,  103.758000f, 143.504000f, 146.231000f,
            //71.823100f, 107.007000f, 212.281000f,  55.310600f, 134.077000f, 179.256000f,  104.848000f, 217.810000f, 159.072000f,  146.129000f, 154.876000f, 245.306000f,
            //127.401000f, 77.454000f, 137.975000f,  45.102300f, 176.529000f, 121.463000f,  129.617000f, 73.791900f, 195.769000f,  170.898000f, 135.248000f, 161.279000f,
            //184.481000f, 135.248000f, 171.000000f,  210.174000f, 110.479000f, 195.769000f,  137.873000f, 202.221000f, 154.488000f,  96.591800f, 232.570000f, 212.281000f,
            //137.873000f, 226.066000f, 177.340000f,  118.771000f, 77.454000f, 204.025000f,  122.909000f, 234.977000f, 204.025000f,  120.829000f, 160.016000f, 146.231000f,
            //96.591800f, 160.016000f, 244.585000f,  113.104000f, 118.735000f, 140.668000f,  96.450000f, 168.273000f, 129.836000f,  104.848000f, 76.831900f, 162.744000f,
            //58.936600f, 126.991000f, 179.256000f,  104.848000f, 135.248000f, 149.555000f,  129.486000f, 151.760000f, 162.744000f,  63.566900f, 250.835000f, 171.740000f,
            //203.923000f, 126.991000f, 191.950000f,  129.617000f, 160.016000f, 244.659000f,  96.591800f, 242.579000f, 205.035000f,  72.225100f, 234.511000f, 146.168000f,
            //146.129000f, 143.504000f, 247.466000f,  88.335600f, 217.810000f, 156.817000f,  71.823100f, 168.273000f, 109.890000f,  93.316300f, 85.710300f, 137.975000f,
            //88.335600f, 151.760000f, 243.071000f,  51.154800f, 143.504000f, 195.769000f,  55.310600f, 160.016000f, 91.718300f,  69.851000f, 110.479000f, 162.744000f,
            //88.335600f, 260.490000f, 162.744000f,  170.898000f, 83.842200f, 220.537000f,  96.591800f, 234.322000f, 159.856000f,  54.601000f, 176.529000f, 212.970000f,
            //137.873000f, 133.215000f, 154.488000f,  137.873000f, 143.504000f, 246.998000f,  228.692000f, 77.454000f, 221.744000f,  184.411000f, 102.223000f, 237.050000f,
            //80.079300f, 217.810000f, 152.071000f,  88.335600f, 118.735000f, 242.717000f,  104.603000f, 259.054000f, 187.430000f,  146.129000f, 93.966500f, 134.786000f,
            //203.923000f, 87.890000f, 228.794000f,  63.566900f, 248.031000f, 162.744000f,  63.731500f, 119.083000f, 146.231000f,  143.088000f, 209.554000f, 171.000000f,
            //64.065200f, 119.600000f, 162.744000f,  55.310600f, 143.504000f, 91.325700f,  113.104000f, 143.504000f, 157.783000f,  113.104000f, 193.041000f, 233.867000f,
            //137.873000f, 76.615300f, 204.025000f,  146.129000f, 85.710300f, 123.448000f,  182.136000f, 176.529000f, 179.256000f,  160.112000f, 85.710300f, 220.537000f,
            //55.310600f, 239.443000f, 179.256000f,  55.310600f, 189.900000f, 121.463000f,  170.898000f, 58.150300f, 146.231000f,  179.154000f, 110.479000f, 159.716000f,
            //154.385000f, 167.975000f, 145.674000f,  71.121800f, 176.529000f, 228.794000f,  63.566900f, 217.810000f, 140.688000f,  47.091000f, 184.656000f, 121.545000f,
            //154.385000f, 160.016000f, 240.887000f,  67.992300f, 176.529000f, 113.206000f,  149.909000f, 60.941500f, 154.488000f,  96.591800f, 110.479000f, 242.226000f,
            //114.321000f, 250.835000f, 171.000000f,  84.376900f, 93.966500f, 154.488000f,  82.233300f, 93.966500f, 204.025000f,  136.674000f, 224.972000f, 204.025000f,
            //104.848000f, 80.486500f, 137.975000f,  57.328200f, 201.298000f, 212.281000f,  129.617000f, 206.319000f, 146.231000f,  51.399900f, 143.504000f, 154.488000f,
            //162.642000f, 126.991000f, 157.437000f,  104.607000f, 242.528000f, 203.994000f,  37.871500f, 176.529000f, 162.744000f,  170.898000f, 95.070100f, 145.437000f,
            //63.566900f, 217.810000f, 208.752000f,  228.692000f, 102.223000f, 237.420000f,  220.334000f, 151.760000f, 228.794000f,  96.591800f, 105.463000f, 129.719000f,
            //217.847000f, 102.223000f, 204.025000f,  203.923000f, 135.248000f, 194.494000f,  54.010700f, 135.248000f, 121.463000f,  55.310600f, 226.066000f, 147.877000f,
            //121.360000f, 136.724000f, 153.348000f,  63.566900f, 130.424000f, 220.537000f,  170.898000f, 179.934000f, 162.744000f,  179.154000f, 118.092000f, 162.937000f,
            //212.179000f, 62.725600f, 162.744000f,  170.898000f, 143.504000f, 161.408000f,  113.104000f, 99.523500f, 237.050000f,  59.487500f, 126.991000f, 171.000000f,
            //134.925000f, 217.810000f, 212.281000f,  138.787000f, 193.041000f, 129.719000f,  63.566900f, 184.785000f, 221.270000f,  113.104000f, 168.273000f, 138.830000f,
            //82.632000f, 93.966500f, 187.512000f,  179.154000f, 135.248000f, 238.512000f,  146.129000f, 122.867000f, 154.488000f,  54.577200f, 135.248000f, 137.975000f,
            //84.605800f, 143.504000f, 113.206000f,  96.591800f, 112.259000f, 137.975000f,  182.599000f, 168.273000f, 212.281000f,  154.385000f, 69.197800f, 191.200000f,
            //88.335600f, 168.273000f, 239.688000f,  60.219600f, 176.529000f, 104.950000f,  179.154000f, 151.760000f, 165.329000f,  113.104000f, 93.966500f, 133.411000f,
            //96.591800f, 82.802900f, 195.769000f,  74.982100f, 102.223000f, 204.025000f,  40.581200f, 176.529000f, 146.231000f,  105.351000f, 77.652200f, 146.231000f,
            //146.129000f, 193.041000f, 140.866000f,  129.617000f, 157.505000f, 146.231000f,  63.557800f, 250.845000f, 170.934000f,  129.617000f, 167.670000f, 129.246000f,
            //105.397000f, 94.062900f, 129.543000f,  159.835000f, 60.941600f, 171.000000f,  146.129000f, 213.169000f, 179.256000f,  137.873000f, 163.375000f, 137.975000f,
            //192.348000f, 143.504000f, 187.512000f,  47.054400f, 176.529000f, 200.821000f,  121.361000f, 244.469000f, 171.000000f,  137.873000f, 71.621100f, 187.512000f,
            //54.336500f, 135.248000f, 187.512000f,  88.335600f, 210.804000f, 153.538000f,  209.553000f, 102.223000f, 179.256000f,  96.589600f, 93.965800f, 228.795000f,
            //113.104000f, 229.588000f, 212.281000f,  146.129000f, 88.217500f, 220.537000f,  64.806900f, 193.724000f, 129.232000f,  80.079300f, 234.322000f, 209.635000f,
            //118.617000f, 102.223000f, 137.975000f,  71.823100f, 135.248000f, 231.084000f,  187.410000f, 61.498400f, 138.950000f,  170.898000f, 184.785000f, 226.190000f,
            //66.391100f, 250.835000f, 187.512000f,  63.566900f, 201.298000f, 216.488000f,  96.591800f, 243.787000f, 204.025000f,  203.923000f, 75.606900f, 146.231000f,
            //187.410000f, 106.112000f, 162.744000f,  187.410000f, 149.324000f, 187.512000f,  113.104000f, 135.868000f, 153.969000f,  207.665000f, 151.760000f, 212.281000f,
            //113.104000f, 256.261000f, 187.512000f,  146.129000f, 64.856300f, 137.975000f,  171.871000f, 201.298000f, 204.025000f,  121.361000f, 187.019000f, 237.050000f,
            //38.826600f, 184.785000f, 154.488000f,  47.054400f, 151.760000f, 98.843000f,  67.450200f, 143.504000f, 228.794000f,  222.858000f, 85.710300f, 179.256000f,
            //129.617000f, 193.041000f, 126.004000f,  59.320200f, 126.991000f, 195.769000f,  88.335600f, 107.315000f, 237.050000f,  129.617000f, 168.273000f, 243.236000f,
            //63.260600f, 242.579000f, 195.769000f,  195.667000f, 77.425500f, 146.301000f,  63.566900f, 164.025000f, 96.693900f,  195.667000f, 126.991000f, 180.931000f,
            //63.823400f, 119.068000f, 195.769000f,  159.356000f, 93.966500f, 137.975000f,  44.142600f, 160.016000f, 146.231000f,  71.823100f, 238.843000f, 204.025000f,
            //104.848000f, 188.743000f, 137.975000f,  162.642000f, 93.966500f, 139.615000f,  46.905700f, 201.671000f, 137.975000f,  217.808000f, 85.710300f, 171.000000f,
            //129.617000f, 217.810000f, 215.406000f,  60.912700f, 176.529000f, 220.537000f,  167.798000f, 184.785000f, 162.744000f,  162.714000f, 201.325000f, 212.328000f,
            //80.079300f, 121.196000f, 129.719000f,  122.544000f, 242.579000f, 170.407000f,  141.892000f, 226.066000f, 187.512000f,  129.617000f, 209.554000f, 222.272000f,
            //44.577000f, 160.016000f, 171.000000f,  203.923000f, 135.248000f, 240.010000f,  104.848000f, 168.273000f, 136.779000f,  170.898000f, 150.118000f, 162.744000f,
            //45.005300f, 168.273000f, 129.719000f,  153.052000f, 184.785000f, 138.944000f,  194.305000f, 126.991000f, 179.256000f,  137.873000f, 74.154000f, 195.769000f,
            //129.617000f, 203.046000f, 137.975000f,  88.335600f, 251.665000f, 196.350000f,  63.566900f, 231.038000f, 204.025000f,  71.823100f, 209.554000f, 216.558000f,
            //162.642000f, 75.256100f, 204.025000f,  146.129000f, 220.762000f, 195.769000f,  236.948000f, 77.454000f, 180.677000f,  137.873000f, 151.760000f, 245.985000f,
            //71.823100f, 126.991000f, 108.911000f,  212.179000f, 77.454000f, 217.463000f,  96.591800f, 82.335300f, 162.744000f,  88.335600f, 110.479000f, 136.236000f,
            //187.410000f, 171.781000f, 187.512000f,  55.310600f, 209.554000f, 137.210000f,  104.848000f, 93.966500f, 231.081000f,  104.848000f, 193.041000f, 140.367000f,
            //195.667000f, 75.443600f, 212.281000f,  121.361000f, 71.213200f, 179.256000f,  41.815800f, 193.041000f, 187.512000f,  88.087900f, 217.946000f, 220.609000f,
            //104.848000f, 85.710300f, 218.295000f,  137.873000f, 67.998100f, 171.000000f,  187.410000f, 93.966500f, 156.787000f,  88.335600f, 93.153500f, 220.537000f,
            //137.873000f, 217.810000f, 171.710000f,  71.823100f, 184.785000f, 227.402000f,  43.544700f, 209.554000f, 154.488000f,  96.591800f, 143.504000f, 249.879000f,
            //236.948000f, 71.988100f, 204.025000f,  55.322400f, 135.275000f, 146.096000f,  203.923000f, 147.269000f, 204.025000f,  80.079300f, 97.030600f, 212.281000f,
            //154.385000f, 85.710300f, 123.858000f,  104.848000f, 143.504000f, 252.058000f,  240.702000f, 77.454000f, 187.512000f,  96.591800f, 242.579000f, 157.446000f,
            //63.566900f, 143.504000f, 224.449000f,  220.435000f, 64.239600f, 179.256000f,  121.361000f, 110.479000f, 246.061000f,  154.385000f, 201.298000f, 218.356000f,
            //49.082500f, 151.760000f, 121.463000f,  129.617000f, 155.963000f, 154.488000f,  187.410000f, 81.794100f, 146.231000f,  88.335600f, 254.707000f, 154.488000f,
            //187.410000f, 135.248000f, 174.232000f,  232.872000f, 69.197800f, 171.000000f,  183.613000f, 60.941600f, 137.975000f,  41.222200f, 201.298000f, 154.488000f,
            //46.392500f, 160.016000f, 113.206000f,  80.079300f, 96.747700f, 195.769000f,  144.694000f, 193.041000f, 137.975000f,  133.842000f, 226.066000f, 171.000000f,
            //137.080000f, 119.166000f, 245.110000f,  55.310600f, 143.504000f, 210.012000f,  154.936000f, 193.041000f, 162.744000f,  80.079300f, 172.453000f, 121.463000f,
            //88.335600f, 90.106400f, 154.488000f,  109.450000f, 77.454000f, 195.769000f,  185.932000f, 176.529000f, 187.512000f,  138.488000f, 69.197800f, 179.256000f,
            //88.335600f, 234.322000f, 210.632000f,  63.566900f, 248.443000f, 187.512000f,  179.154000f, 102.223000f, 156.086000f,  187.410000f, 124.274000f, 171.000000f,
            //55.310600f, 215.338000f, 204.025000f,  68.790200f, 110.479000f, 146.231000f,  87.225500f, 143.504000f, 121.463000f,  105.839000f, 77.716000f, 187.512000f,
            //203.923000f, 143.504000f, 238.812000f,  87.145700f, 93.966500f, 220.537000f,  121.361000f, 77.454000f, 140.387000f,  51.299000f, 143.504000f, 171.000000f,
            //148.848000f, 193.041000f, 146.231000f,  195.667000f, 151.760000f, 234.317000f,  80.079300f, 226.066000f, 213.488000f,  103.799000f, 77.454000f, 162.744000f,
            //104.848000f, 126.991000f, 250.732000f,  228.692000f, 93.966500f, 236.401000f,  96.591800f, 260.312000f, 187.512000f,  92.485600f, 242.579000f, 154.488000f,
            //42.249300f, 209.554000f, 162.744000f,  212.179000f, 113.471000f, 237.050000f,  126.420000f, 168.273000f, 129.719000f,  121.361000f, 168.273000f, 243.915000f,
            //195.667000f, 102.711000f, 236.839000f,  129.617000f, 90.036600f, 137.975000f,  199.452000f, 143.504000f, 195.769000f,  179.154000f, 135.248000f, 166.231000f,
            //113.104000f, 209.554000f, 151.835000f,  88.335600f, 242.579000f, 151.934000f,  172.977000f, 93.966500f, 146.231000f,  170.898000f, 64.928000f, 187.512000f,
            //228.692000f, 77.680800f, 170.286000f,  64.658900f, 193.041000f, 219.809000f,  47.055700f, 176.524000f, 113.215000f,  84.194200f, 168.273000f, 121.463000f,
            //50.708600f, 160.016000f, 204.025000f,  179.154000f, 67.333100f, 195.769000f,  121.361000f, 72.613600f, 187.512000f,  104.848000f, 183.631000f, 236.576000f,
            //210.773000f, 135.248000f, 204.025000f,  146.562000f, 177.237000f, 237.156000f,  63.566900f, 151.760000f, 93.789900f,  207.281000f, 118.735000f, 195.769000f,
            //242.910000f, 77.454000f, 195.769000f,  93.009500f, 209.554000f, 154.488000f,  142.689000f, 69.197800f, 129.719000f,  146.129000f, 210.285000f, 213.043000f,
            //104.848000f, 205.461000f, 228.794000f,  80.079300f, 184.785000f, 231.906000f,  220.435000f, 87.906600f, 179.256000f,  81.598200f, 259.091000f, 162.744000f,
            //113.104000f, 74.999000f, 187.512000f,  113.104000f, 168.273000f, 244.423000f,  121.361000f, 209.554000f, 225.075000f,  47.054400f, 220.434000f, 162.744000f,
            //170.898000f, 193.041000f, 217.011000f,  162.735000f, 209.571000f, 187.292000f,  71.544900f, 201.653000f, 220.661000f,  113.104000f, 242.579000f, 202.006000f,
            //170.898000f, 62.060900f, 179.256000f,  220.435000f, 70.939100f, 204.025000f,  47.000500f, 151.760000f, 105.978000f,  45.225300f, 217.810000f, 171.000000f,
            //186.111000f, 151.760000f, 171.000000f,  88.347200f, 135.217000f, 129.676000f,  129.617000f, 230.498000f, 204.025000f,  179.154000f, 62.650600f, 187.512000f,
            //146.149000f, 168.263000f, 137.961000f,  121.361000f, 70.393500f, 171.000000f,  83.726500f, 217.810000f, 154.488000f,  104.848000f, 110.479000f, 243.519000f,
            //55.140000f, 135.094000f, 96.557800f,  113.104000f, 92.936100f, 228.794000f,  71.823100f, 249.478000f, 154.488000f,  215.781000f, 135.248000f, 212.281000f,
            //41.720100f, 209.554000f, 171.000000f,  162.642000f, 93.966500f, 228.875000f,  162.642000f, 193.041000f, 167.062000f,  71.823100f, 107.109000f, 195.769000f,
            //71.823100f, 118.735000f, 125.865000f,  48.739300f, 151.760000f, 179.256000f,  220.435000f, 84.083700f, 228.794000f,  220.435000f, 67.525100f, 195.769000f,
            //80.079300f, 217.810000f, 216.915000f,  129.617000f, 221.337000f, 212.281000f,  88.335600f, 174.142000f, 237.050000f,  59.916400f, 126.991000f, 146.231000f,
            //63.566900f, 173.371000f, 104.950000f,  129.216000f, 242.107000f, 179.256000f,  119.069000f, 93.966500f, 228.794000f,  179.154000f, 93.966500f, 150.865000f,
            //113.104000f, 77.984800f, 138.673000f,  79.537900f, 151.760000f, 105.558000f,  97.908000f, 85.710300f, 212.281000f,  121.361000f, 94.167500f, 228.494000f,
            //71.823100f, 168.273000f, 231.425000f,  69.630000f, 110.479000f, 179.256000f,  60.405300f, 126.991000f, 154.488000f,  137.873000f, 193.496000f, 129.389000f,
            //121.361000f, 110.479000f, 140.209000f,  154.385000f, 67.465100f, 187.512000f,  203.923000f, 75.071100f, 212.281000f,  55.204400f, 194.140000f, 212.445000f,
            //185.392000f, 176.529000f, 204.025000f,  113.104000f, 204.627000f, 228.794000f,  71.823100f, 106.881000f, 146.231000f,  144.963000f, 176.529000f, 129.719000f,
            //224.957000f, 110.479000f, 237.050000f,  96.591800f, 198.112000f, 146.231000f,  83.411600f, 93.966500f, 137.975000f,  162.642000f, 176.529000f, 233.132000f,
            //71.823100f, 226.066000f, 210.411000f,  48.309900f, 143.504000f, 104.950000f,  129.617000f, 211.828000f, 162.744000f,  187.369000f, 152.416000f, 179.256000f,
            //113.104000f, 193.041000f, 135.595000f,  212.179000f, 69.197800f, 200.853000f,  74.142200f, 259.091000f, 179.256000f,  154.385000f, 206.092000f, 212.281000f,
            //38.798200f, 184.785000f, 175.522000f,  71.365000f, 200.590000f, 138.365000f,  154.385000f, 80.906700f, 212.281000f,  179.154000f, 110.479000f, 241.651000f,
            //220.435000f, 143.504000f, 224.578000f,  232.651000f, 69.197800f, 195.769000f,  85.761900f, 110.479000f, 237.050000f,  175.694000f, 201.298000f, 187.512000f,
            //60.603400f, 126.991000f, 212.281000f,  47.524900f, 152.899000f, 171.000000f,  104.848000f, 231.882000f, 212.281000f,  137.873000f, 157.863000f, 154.488000f,
            //55.310600f, 209.554000f, 206.913000f,  211.950000f, 126.991000f, 204.298000f,  204.031000f, 60.920500f, 162.231000f,  187.410000f, 85.710300f, 224.867000f,
            //195.667000f, 60.941500f, 182.910000f,  113.104000f, 184.785000f, 237.537000f,  170.898000f, 57.864900f, 162.744000f,  179.154000f, 160.016000f, 168.564000f,
            //104.848000f, 99.886600f, 237.050000f,  76.297500f, 102.223000f, 154.488000f,  195.667000f, 59.589600f, 171.000000f,  80.079300f, 201.298000f, 144.075000f,
            //92.513900f, 85.710300f, 146.231000f,  146.387000f, 218.272000f, 186.470000f,  113.104000f, 226.066000f, 159.812000f,  154.385000f, 105.980000f, 146.231000f,
            //104.848000f, 226.066000f, 160.539000f,  38.993100f, 200.434000f, 171.000000f,  55.310600f, 238.161000f, 171.000000f,  71.823100f, 193.041000f, 133.640000f,
            //154.385000f, 118.735000f, 154.870000f,  63.566900f, 124.158000f, 104.950000f,  104.848000f, 91.695100f, 129.719000f,  58.045000f, 135.248000f, 212.281000f,
            //162.642000f, 60.941500f, 141.294000f,  129.951000f, 102.175000f, 237.119000f,  80.079300f, 205.220000f, 146.231000f,  154.385000f, 71.718000f, 195.769000f,
            //170.898000f, 193.041000f, 173.367000f,  162.642000f, 151.760000f, 242.061000f,  79.264300f, 108.881000f, 228.794000f,  187.410000f, 102.478000f, 236.920000f,
            //187.410000f, 149.084000f, 179.256000f,  55.026600f, 168.347000f, 96.630600f,  154.385000f, 61.063200f, 146.864000f,  80.079300f, 234.322000f, 149.324000f,
            //129.617000f, 87.400600f, 220.537000f,  146.129000f, 62.617600f, 162.744000f,  104.848000f, 176.529000f, 240.483000f,  121.361000f, 160.016000f, 143.621000f,
            //146.129000f, 160.016000f, 154.811000f,  162.642000f, 93.890400f, 228.794000f,  214.041000f, 110.479000f, 204.025000f,  187.410000f, 168.273000f, 194.154000f,
            //104.625000f, 209.692000f, 154.398000f,  80.079300f, 100.476000f, 137.975000f,  146.129000f, 195.319000f, 146.231000f,  212.172000f, 118.659000f, 204.040000f,
            //212.179000f, 151.760000f, 236.786000f,  64.949000f, 234.322000f, 203.418000f,  113.104000f, 151.760000f, 249.416000f,  67.800000f, 151.760000f, 96.693900f,
            //228.692000f, 66.810800f, 171.000000f,  241.213000f, 77.454000f, 204.025000f,  137.873000f, 85.710300f, 217.676000f,  234.195000f, 85.710300f, 220.537000f,
            //104.848000f, 102.223000f, 238.918000f,  48.816600f, 151.760000f, 129.719000f,  179.154000f, 184.785000f, 217.147000f,  154.414000f, 135.195000f, 245.314000f,
            //88.335600f, 143.504000f, 124.453000f,  66.481100f, 250.835000f, 162.744000f,  154.385000f, 199.399000f, 220.537000f,  96.591800f, 259.091000f, 189.121000f,
            //216.701000f, 126.991000f, 212.281000f,  162.642000f, 110.479000f, 240.905000f,  183.520000f, 168.273000f, 204.025000f,  55.310600f, 198.992000f, 129.719000f,
            //179.154000f, 176.529000f, 224.529000f,  57.791700f, 242.579000f, 171.000000f,  48.667000f, 226.066000f, 179.256000f,  203.923000f, 66.001600f, 195.769000f,
            //88.335600f, 102.223000f, 232.971000f,  50.560800f, 143.504000f, 187.512000f,  195.667000f, 85.710300f, 155.678000f,  165.028000f, 184.785000f, 154.488000f,
            //162.642000f, 110.479000f, 149.496000f,  146.129000f, 194.167000f, 228.794000f,  47.054400f, 151.760000f, 154.487000f,  71.823100f, 234.322000f, 206.945000f,
            //52.026300f, 226.066000f, 154.488000f,  69.577000f, 110.479000f, 187.512000f,  224.590000f, 126.991000f, 228.794000f,  195.667000f, 85.710300f, 225.321000f,
            //162.642000f, 135.248000f, 158.201000f,  154.385000f, 93.966500f, 135.502000f,  65.649000f, 184.785000f, 121.463000f,  104.848000f, 110.479000f, 135.100000f,
            //170.898000f, 118.735000f, 157.914000f,  47.168000f, 160.846000f, 195.769000f,  170.723000f, 60.926300f, 137.931000f,  165.209000f, 184.785000f, 228.794000f,
            //212.179000f, 62.021800f, 179.256000f,  229.412000f, 77.454000f, 171.000000f,  43.738400f, 168.273000f, 137.975000f,  204.358000f, 136.577000f, 195.769000f,
            //146.129000f, 126.991000f, 155.951000f,  232.009000f, 85.710300f, 204.025000f,  203.833000f, 102.028000f, 171.108000f,  162.642000f, 143.504000f, 243.112000f,
            //92.275700f, 85.710300f, 162.744000f,  154.385000f, 210.885000f, 204.025000f,  148.735000f, 193.041000f, 228.794000f,  88.335600f, 250.835000f, 152.259000f,
            //170.898000f, 180.517000f, 228.794000f,  172.125000f, 85.710300f, 137.975000f,  129.617000f, 70.708300f, 146.231000f,  121.361000f, 250.835000f, 188.809000f,
            //88.335600f, 263.569000f, 179.256000f,  104.628000f, 242.579000f, 162.915000f,  43.657100f, 184.785000f, 129.719000f,  220.435000f, 77.454000f, 159.808000f,
            //121.361000f, 109.429000f, 245.306000f,  210.847000f, 85.574400f, 228.794000f,  46.313200f, 160.016000f, 104.950000f,  113.104000f, 217.810000f, 157.739000f,
            //88.335600f, 188.391000f, 137.975000f,  92.574200f, 151.760000f, 245.306000f,  154.385000f, 160.016000f, 158.722000f,  129.617000f, 211.833000f, 220.537000f,
            //172.681000f, 77.454000f, 212.281000f,  103.740000f, 250.835000f, 162.744000f,  137.306000f, 200.960000f, 139.016000f,  217.674000f, 143.504000f, 220.537000f,
            //84.744700f, 151.760000f, 113.206000f,  104.848000f, 125.901000f, 146.885000f,  121.361000f, 73.021100f, 146.231000f,  55.310600f, 135.248000f, 171.486000f,
            //137.873000f, 135.248000f, 155.265000f,  88.335600f, 89.398800f, 146.231000f,  195.667000f, 60.116500f, 179.256000f,  146.129000f, 91.318900f, 129.719000f,
            //71.823100f, 193.041000f, 224.532000f,  88.335600f, 88.789300f, 195.769000f,  60.165700f, 168.273000f, 220.537000f,  161.107000f, 184.785000f, 146.231000f,
            //162.642000f, 77.454000f, 125.941000f,  71.823100f, 106.225000f, 204.025000f,  80.079300f, 98.009000f, 146.231000f,  47.556400f, 184.785000f, 202.715000f,
            //56.791900f, 217.810000f, 204.025000f,  195.667000f, 77.454000f, 215.484000f,  54.991600f, 168.273000f, 212.800000f,  113.104000f, 204.405000f, 146.231000f,
            //187.410000f, 154.608000f, 204.025000f,  187.410000f, 93.966500f, 231.037000f,  49.326100f, 226.066000f, 187.512000f,  78.597500f, 118.735000f, 129.719000f,
            //104.453000f, 102.223000f, 129.961000f,  146.129000f, 62.248200f, 154.488000f,  71.823100f, 254.493000f, 187.512000f,  121.363000f, 193.040000f, 129.720000f,
            //113.104000f, 196.115000f, 137.975000f,  212.179000f, 71.003000f, 204.025000f,  187.410000f, 85.710300f, 150.420000f,  203.923000f, 102.223000f, 235.913000f,
            //154.385000f, 59.531800f, 162.744000f,  173.550000f, 135.248000f, 162.744000f,  228.692000f, 88.177000f, 204.025000f,  63.566900f, 121.785000f, 113.206000f,
            //104.848000f, 77.454000f, 150.081000f,  104.848000f, 118.735000f, 141.896000f,  80.079300f, 102.223000f, 223.222000f,  91.326400f, 85.710300f, 179.256000f,
            //80.079300f, 184.785000f, 131.176000f,  154.385000f, 77.454000f, 121.889000f,  48.535000f, 143.504000f, 96.693900f,  76.008100f, 102.223000f, 162.744000f,
            //55.330700f, 135.297000f, 171.000000f,  88.335600f, 234.322000f, 154.788000f,  121.361000f, 143.504000f, 159.379000f,  71.823100f, 242.579000f, 201.213000f,
            //113.104000f, 77.454000f, 200.093000f,  62.252500f, 135.248000f, 220.537000f,  113.104000f, 176.529000f, 242.402000f,  43.083700f, 193.041000f, 137.975000f,
            //204.684000f, 110.479000f, 179.256000f,  71.823100f, 184.785000f, 126.672000f,  55.310600f, 133.646000f, 129.719000f,  112.637000f, 250.750000f, 195.669000f,
            //137.634000f, 217.087000f, 171.000000f,  80.079300f, 226.066000f, 151.765000f,  80.079300f, 160.016000f, 110.780000f,  46.924600f, 218.339000f, 187.512000f,
            //88.335600f, 168.273000f, 124.445000f,  55.310600f, 217.810000f, 202.634000f,  121.361000f, 234.322000f, 164.582000f,  187.410000f, 160.016000f, 175.829000f,
            //80.079300f, 228.777000f, 212.281000f,  137.873000f, 203.838000f, 162.744000f,  212.179000f, 90.875900f, 171.000000f,  170.604000f, 69.197800f, 129.737000f,
            //154.385000f, 143.504000f, 246.287000f,  183.328000f, 168.273000f, 220.537000f,  137.873000f, 184.785000f, 123.283000f,  102.872000f, 160.016000f, 137.975000f,
            //71.823100f, 160.016000f, 233.820000f,  220.103000f, 135.248000f, 221.285000f,  71.823100f, 248.515000f, 195.769000f,  64.719700f, 151.760000f, 227.647000f,
            //40.734400f, 184.785000f, 146.231000f,  42.904200f, 201.298000f, 187.512000f,  175.859000f, 176.529000f, 171.000000f,  121.361000f, 86.168700f, 219.904000f,
            //170.199000f, 159.792000f, 236.962000f,  76.274300f, 226.066000f, 212.281000f,  179.154000f, 60.941500f, 183.081000f,  229.880000f, 102.223000f, 237.050000f,
            //137.873000f, 113.337000f, 146.231000f,  55.310600f, 234.322000f, 193.630000f,  170.898000f, 206.098000f, 195.769000f,  154.385000f, 211.123000f, 187.512000f,
            //146.129000f, 193.041000f, 229.735000f,  137.873000f, 151.760000f, 160.203000f,  113.104000f, 126.991000f, 251.033000f,  69.385200f, 110.479000f, 195.769000f,
            //39.932500f, 193.041000f, 179.256000f,  137.873000f, 184.005000f, 236.754000f,  53.225700f, 143.504000f, 204.025000f,  228.692000f, 106.871000f, 237.050000f,
            //47.487200f, 153.265000f, 137.975000f,  129.617000f, 76.135900f, 204.025000f,  121.361000f, 157.099000f, 154.488000f,  146.129000f, 102.223000f, 143.108000f,
            //71.823100f, 110.479000f, 218.639000f,  220.435000f, 83.638000f, 171.000000f,  170.898000f, 201.622000f, 204.650000f,  59.282200f, 126.991000f, 129.719000f,
            //220.435000f, 66.289400f, 162.744000f,  179.154000f, 168.273000f, 172.835000f,  63.445700f, 118.735000f, 204.025000f,  220.435000f, 151.447000f, 228.794000f,
            //41.704500f, 184.785000f, 137.975000f,  63.566900f, 186.323000f, 121.463000f,  96.591800f, 126.991000f, 143.590000f,  154.385000f, 102.223000f, 235.093000f,
            //91.697200f, 85.710300f, 187.512000f,  137.788000f, 93.996900f, 137.949000f,  69.259700f, 110.479000f, 212.281000f,  170.898000f, 174.564000f, 162.744000f,
            //146.129000f, 63.262400f, 146.231000f,  37.602900f, 193.041000f, 171.000000f,  113.104000f, 184.785000f, 131.886000f,  104.848000f, 222.054000f, 220.537000f,
            //203.923000f, 85.710300f, 226.987000f,  129.617000f, 93.966500f, 139.492000f,  121.659000f, 251.033000f, 187.512000f,  137.873000f, 209.554000f, 218.915000f,
            //195.667000f, 66.065800f, 195.769000f,  162.642000f, 84.977500f, 129.719000f,  146.129000f, 184.785000f, 234.918000f,  75.592000f, 102.223000f, 195.769000f,
            //75.994600f, 102.223000f, 187.512000f,  71.823100f, 107.755000f, 162.744000f,  186.664000f, 176.529000f, 195.769000f,  179.154000f, 176.529000f, 175.116000f,
            //121.361000f, 176.529000f, 126.716000f,  137.873000f, 77.454000f, 206.135000f,  195.667000f, 118.735000f, 175.363000f,  220.435000f, 65.095500f, 187.512000f,
            //51.079400f, 143.504000f, 121.463000f,  212.179000f, 126.991000f, 239.567000f,  237.063000f, 77.454000f, 212.674000f,  63.566900f, 242.579000f, 154.802000f,
            //71.035800f, 217.810000f, 212.602000f,  88.920300f, 143.504000f, 244.952000f,  154.385000f, 204.258000f, 171.000000f,  104.778000f, 77.423900f, 179.844000f,
            //47.054400f, 157.097000f, 113.206000f,  113.104000f, 185.602000f, 237.050000f,  52.320500f, 234.322000f, 179.256000f,  163.075000f, 167.550000f, 153.802000f,
            //154.385000f, 209.554000f, 206.788000f,  236.986000f, 69.157400f, 179.626000f,  55.310600f, 201.298000f, 210.590000f,  47.219400f, 152.244000f, 146.231000f,
            //179.154000f, 82.408200f, 220.537000f,  83.331600f, 93.966500f, 162.744000f,  84.733500f, 259.091000f, 187.512000f,  162.642000f, 85.710300f, 130.931000f,
            //187.410000f, 160.016000f, 229.298000f,  50.121400f, 168.273000f, 204.025000f,  137.873000f, 169.454000f, 129.719000f,  212.179000f, 118.735000f, 238.128000f,
            //113.104000f, 85.710300f, 133.348000f,  196.416000f, 151.972000f, 203.596000f,  55.310600f, 226.066000f, 198.585000f,  228.692000f, 110.479000f, 236.108000f,
            //143.280000f, 217.810000f, 179.256000f,  137.873000f, 155.933000f, 245.306000f,  129.617000f, 176.529000f, 241.730000f,  96.591800f, 110.479000f, 135.814000f,
            //129.677000f, 234.472000f, 170.900000f,  121.361000f, 70.266600f, 162.744000f,  113.104000f, 160.016000f, 149.105000f,  129.617000f, 135.248000f, 249.340000f,
            //88.335600f, 89.274500f, 162.744000f,  212.179000f, 85.710300f, 164.687000f,  212.179000f, 146.343000f, 212.281000f,  177.307000f, 184.785000f, 220.537000f,
            //181.769000f, 176.529000f, 220.537000f,  104.848000f, 151.760000f, 249.211000f,  82.052300f, 160.016000f, 113.206000f,  63.784100f, 119.070000f, 187.512000f,
            //80.079300f, 110.479000f, 231.148000f,  161.663000f, 209.554000f, 195.769000f,  88.335600f, 193.041000f, 231.771000f,  79.861900f, 143.504000f, 105.355000f,
            //82.562600f, 93.966500f, 195.769000f,  121.361000f, 76.965900f, 204.025000f,  80.079300f, 97.059000f, 187.512000f,  203.923000f, 111.681000f, 179.256000f,
            //137.873000f, 160.016000f, 149.003000f,  88.335600f, 90.392600f, 212.281000f,  221.335000f, 126.991000f, 237.456000f,  55.310600f, 183.738000f, 113.206000f,
            //47.040300f, 217.841000f, 154.408000f,  129.617000f, 77.454000f, 207.050000f,  88.335600f, 151.760000f, 121.463000f,  46.989800f, 159.738000f, 129.719000f,
            //96.591800f, 208.587000f, 154.488000f,  121.361000f, 226.280000f, 212.281000f,  228.692000f, 76.790800f, 220.537000f,  162.642000f, 60.941500f, 171.789000f,
            //187.410000f, 164.525000f, 179.256000f,  162.642000f, 189.157000f, 162.744000f,  110.428000f, 143.504000f, 154.488000f,  154.385000f, 86.360000f, 219.873000f,
            //59.199500f, 126.991000f, 137.975000f,  181.213000f, 184.785000f, 212.281000f,  88.335600f, 176.529000f, 236.032000f,  220.435000f, 145.018000f, 237.050000f,
            //232.477000f, 77.454000f, 220.537000f,  162.642000f, 135.248000f, 242.125000f,  50.660600f, 135.248000f, 104.950000f,  57.359600f, 234.322000f, 154.488000f,
            //79.956800f, 127.167000f, 121.834000f,  228.692000f, 112.484000f, 228.794000f,  80.079300f, 168.273000f, 117.767000f,  171.380000f, 110.479000f, 154.179000f,
            //203.527000f, 160.765000f, 228.794000f,  80.079300f, 102.223000f, 136.742000f,  61.112700f, 168.273000f, 96.693900f,  93.788900f, 250.835000f, 154.488000f,
            //134.342000f, 234.322000f, 179.256000f,  162.642000f, 60.650800f, 171.000000f,  129.643000f, 242.611000f, 187.839000f,  113.104000f, 176.529000f, 132.926000f,
            //82.677700f, 93.966500f, 179.256000f,  179.154000f, 155.462000f, 237.050000f,  96.591800f, 263.630000f, 179.256000f,  110.399000f, 184.785000f, 237.050000f,
            //96.591800f, 201.298000f, 230.220000f,  162.642000f, 174.870000f, 146.231000f,  96.591800f, 184.785000f, 137.609000f,  215.051000f, 93.966500f, 179.256000f,
            //137.873000f, 110.479000f, 144.655000f,  129.617000f, 184.785000f, 122.049000f,  96.591800f, 262.852000f, 171.000000f,  146.129000f, 168.273000f, 240.044000f,
            //113.104000f, 160.016000f, 246.016000f,  44.441300f, 193.041000f, 195.769000f,  47.054400f, 157.229000f, 179.256000f,  71.823100f, 250.835000f, 156.200000f,
            //129.617000f, 201.298000f, 135.345000f,  146.098000f, 127.017000f, 245.298000f,  61.130100f, 226.066000f, 204.025000f,  121.361000f, 163.348000f, 137.975000f,
            //113.186000f, 110.518000f, 245.291000f,  154.385000f, 95.879600f, 137.975000f,  170.898000f, 77.454000f, 211.685000f,  72.341800f, 226.066000f, 145.822000f,
            //183.151000f, 184.785000f, 204.025000f,  137.873000f, 102.223000f, 141.793000f,  203.923000f, 60.499200f, 171.000000f,  137.873000f, 217.810000f, 210.490000f,
            //203.923000f, 80.255600f, 220.537000f,  170.898000f, 102.223000f, 149.712000f,  195.667000f, 159.087000f, 220.537000f,  212.179000f, 68.223800f, 153.314000f,
            //76.349500f, 102.223000f, 179.256000f,  150.330000f, 193.041000f, 154.488000f,  121.361000f, 100.379000f, 237.050000f,  146.129000f, 198.308000f, 162.744000f,
            //55.310600f, 201.298000f, 131.598000f,  39.905000f, 184.785000f, 179.256000f,  121.361000f, 152.354000f, 162.879000f,  187.036000f, 160.016000f, 187.512000f,
            //88.335600f, 118.735000f, 139.500000f,  200.865000f, 126.991000f, 187.512000f,  137.873000f, 81.320300f, 212.281000f,  162.642000f, 117.842000f, 154.488000f,
            //38.845300f, 176.862000f, 170.644000f,  88.335600f, 93.966500f, 222.135000f,  203.923000f, 69.197800f, 201.548000f,  220.487000f, 126.991000f, 220.390000f,
            //207.055000f, 143.504000f, 204.025000f,  122.571000f, 209.935000f, 153.910000f,  132.582000f, 69.197800f, 146.231000f,  46.145600f, 160.016000f, 179.256000f,
            //236.948000f, 83.804400f, 195.769000f,  85.586100f, 135.248000f, 121.463000f,  179.154000f, 60.351600f, 137.975000f,  37.556100f, 184.785000f, 171.000000f,
            //78.300100f, 184.175000f, 129.719000f,  96.591800f, 82.717100f, 154.488000f,  214.880000f, 69.197800f, 154.488000f,  162.642000f, 143.504000f, 159.338000f,
            //92.137800f, 85.710300f, 195.769000f,  63.566900f, 226.066000f, 205.883000f,  184.189000f, 110.479000f, 162.744000f,  79.760700f, 209.946000f, 220.678000f,
            //170.898000f, 126.991000f, 240.802000f,  187.410000f, 58.507600f, 171.000000f,  212.179000f, 97.908900f, 179.256000f,  63.566900f, 234.322000f, 147.511000f,
            //96.591800f, 221.484000f, 220.537000f,  121.361000f, 77.454000f, 205.210000f,  154.385000f, 110.479000f, 239.523000f,  69.923700f, 126.991000f, 104.950000f,
            //170.898000f, 143.504000f, 240.830000f,  121.361000f, 184.785000f, 125.828000f,  121.361000f, 102.223000f, 139.160000f,  209.499000f, 93.966500f, 171.000000f,
            //129.617000f, 110.479000f, 243.590000f,  187.410000f, 168.273000f, 183.864000f,  134.184000f, 209.554000f, 220.537000f,  137.873000f, 176.529000f, 123.826000f,
            //38.798200f, 193.041000f, 175.495000f,  212.179000f, 110.479000f, 200.662000f,  105.086000f, 151.760000f, 145.848000f,  63.568400f, 135.247000f, 96.693500f,
            //126.462000f, 201.298000f, 228.794000f,  203.923000f, 62.640900f, 187.512000f,  59.574200f, 126.991000f, 204.025000f,  195.667000f, 59.032700f, 162.744000f,
            //135.944000f, 234.322000f, 187.512000f,  221.860000f, 118.735000f, 220.537000f,  212.179000f, 143.504000f, 210.332000f,  141.395000f, 226.066000f, 195.769000f,
            //63.566900f, 176.529000f, 108.552000f,  63.566900f, 143.504000f, 94.364100f,  146.129000f, 106.543000f, 237.050000f,  137.873000f, 102.223000f, 235.196000f,
            //129.118000f, 69.132200f, 162.744000f,  129.617000f, 176.529000f, 122.658000f,  154.385000f, 90.773900f, 129.719000f,  195.667000f, 81.423800f, 220.537000f,
            //176.876000f, 102.223000f, 154.488000f,  187.410000f, 71.500400f, 204.025000f,  99.635100f, 151.760000f, 137.975000f,  40.718600f, 176.529000f, 179.256000f,
            //47.054400f, 193.041000f, 201.307000f,  188.001000f, 160.161000f, 228.794000f,  96.591800f, 157.609000f, 245.306000f,  96.591800f, 84.016100f, 137.975000f,
            //170.898000f, 126.991000f, 160.746000f,  203.923000f, 118.735000f, 238.286000f,  195.667000f, 69.197800f, 140.528000f,  55.444800f, 127.154000f, 113.206000f,
            //104.848000f, 184.785000f, 136.487000f,  179.154000f, 81.155900f, 137.975000f,  121.361000f, 70.634500f, 154.488000f,  47.137700f, 167.515000f, 105.312000f,
            //59.605400f, 160.016000f, 220.537000f,  121.361000f, 193.041000f, 234.305000f,  228.692000f, 110.479000f, 226.716000f,  179.154000f, 99.244500f, 154.488000f,
            //38.798200f, 184.785000f, 154.557000f,  47.054400f, 168.273000f, 198.592000f,  183.038000f, 184.785000f, 187.512000f,  170.898000f, 73.760300f, 204.025000f,
            //137.873000f, 126.991000f, 152.048000f,  195.667000f, 143.504000f, 191.725000f,  63.566900f, 242.731000f, 195.933000f,  113.104000f, 212.915000f, 154.488000f,
            //63.566900f, 232.354000f, 146.231000f,  121.361000f, 82.293400f, 137.975000f,  41.304700f, 168.273000f, 171.000000f,  96.591800f, 135.248000f, 250.835000f,
            //179.154000f, 60.941500f, 137.068000f,  83.867400f, 226.066000f, 154.488000f,  55.686900f, 135.652000f, 154.488000f,  203.923000f, 159.839000f, 220.643000f,
            //121.361000f, 151.760000f, 248.277000f,  154.385000f, 191.305000f, 154.488000f,  129.617000f, 126.991000f, 148.737000f,  226.675000f, 93.966500f, 212.281000f,
            //180.984000f, 160.016000f, 204.025000f,  146.129000f, 135.248000f, 247.435000f,  137.725000f, 86.120400f, 129.457000f,  228.692000f, 73.554800f, 212.281000f,
            //229.806000f, 110.479000f, 228.794000f,  96.591800f, 193.041000f, 142.774000f,  63.566900f, 118.665000f, 202.796000f,  137.873000f, 110.479000f, 240.814000f,
            //187.410000f, 160.016000f, 217.543000f,  187.410000f, 57.474900f, 162.744000f,  187.410000f, 75.984500f, 212.281000f,  113.104000f, 251.521000f, 170.663000f,
            //187.410000f, 76.004900f, 137.975000f,  129.617000f, 71.982200f, 187.512000f,  121.361000f, 229.370000f, 162.744000f,  176.192000f, 201.298000f, 195.769000f,
            //212.179000f, 93.966500f, 234.753000f,  168.146000f, 193.041000f, 171.000000f,  162.642000f, 126.991000f, 242.232000f,  144.641000f, 184.785000f, 129.719000f,
            //129.617000f, 85.710300f, 136.616000f,  192.100000f, 69.197800f, 137.975000f,  136.297000f, 135.248000f, 154.488000f,  137.873000f, 168.273000f, 131.232000f,
            //71.823100f, 242.579000f, 148.918000f,  162.642000f, 193.041000f, 222.702000f,  231.063000f, 85.710300f, 195.769000f,  100.295000f, 160.016000f, 245.306000f,
            //187.410000f, 135.248000f, 237.413000f,  104.848000f, 113.905000f, 137.975000f,  39.545400f, 193.041000f, 154.488000f,  170.898000f, 118.735000f, 240.812000f,
            //187.410000f, 151.760000f, 235.849000f,  198.166000f, 135.248000f, 187.512000f,  187.410000f, 57.866300f, 146.231000f,  165.098000f, 102.223000f, 146.231000f,
            //220.435000f, 69.197800f, 199.814000f,  146.129000f, 163.535000f, 146.231000f,  162.642000f, 209.260000f, 195.769000f,  220.435000f, 98.031300f, 237.050000f,
            //154.385000f, 143.504000f, 159.037000f,  220.435000f, 69.197800f, 158.853000f,  69.776600f, 135.248000f, 228.794000f,  184.179000f, 176.529000f, 212.281000f,
            //170.898000f, 135.248000f, 240.186000f,  104.848000f, 76.749000f, 171.000000f,  53.031600f, 226.066000f, 195.769000f,  63.587200f, 118.782000f, 179.256000f,
            //203.923000f, 149.452000f, 237.050000f,  195.667000f, 112.724000f, 171.000000f,  142.851000f, 126.991000f, 154.488000f,  223.041000f, 102.223000f, 212.281000f,
            //185.636000f, 151.760000f, 187.512000f,  88.335600f, 259.091000f, 188.538000f,  71.823100f, 108.024000f, 171.000000f,  47.054400f, 150.915000f, 104.950000f,
            //113.104000f, 257.213000f, 179.256000f,  121.361000f, 74.255500f, 195.769000f,  154.190000f, 168.132000f, 237.004000f,  179.139000f, 193.009000f, 203.971000f,
            //129.617000f, 69.197800f, 167.327000f,  195.667000f, 118.735000f, 238.831000f,  88.257400f, 160.016000f, 121.539000f,  51.190600f, 143.504000f, 162.744000f,
            //104.848000f, 226.066000f, 217.277000f,  195.667000f, 146.836000f, 195.769000f,  71.823100f, 188.280000f, 129.719000f,  88.335600f, 128.397000f, 245.306000f,
            //96.591800f, 176.529000f, 132.724000f,  109.266000f, 160.016000f, 146.231000f,  96.591800f, 251.578000f, 196.624000f,  69.908400f, 110.479000f, 171.000000f,
            //187.410000f, 69.197800f, 134.652000f,  228.692000f, 85.710300f, 232.180000f,  137.873000f, 95.146400f, 227.371000f,  170.898000f, 93.966500f, 229.195000f,
            //113.104000f, 73.093000f, 171.000000f,  226.032000f, 118.735000f, 228.794000f,  93.777300f, 85.710300f, 204.025000f,  176.282000f, 168.273000f, 171.000000f,
            //154.385000f, 149.982000f, 245.306000f,  129.617000f, 126.991000f, 250.209000f,  96.591800f, 226.066000f, 217.124000f,  179.154000f, 85.710300f, 223.703000f,
            //104.848000f, 162.000000f, 245.306000f,  179.154000f, 88.417200f, 146.231000f,  187.410000f, 110.479000f, 164.693000f,  60.182600f, 126.991000f, 162.744000f,
            //63.667100f, 242.405000f, 154.488000f,  63.705100f, 118.960000f, 137.975000f,  162.642000f, 184.785000f, 229.812000f,  228.692000f, 86.977800f, 195.769000f,
            //137.873000f, 143.504000f, 157.978000f,  154.385000f, 135.248000f, 157.668000f,  88.335600f, 135.248000f, 245.999000f,  154.385000f, 176.529000f, 235.116000f,
            //195.456000f, 77.454000f, 146.231000f,  65.023100f, 160.016000f, 95.996400f,  121.361000f, 234.322000f, 205.244000f,  55.458000f, 127.222000f, 104.950000f,
            //187.410000f, 161.332000f, 220.537000f,  129.617000f, 151.760000f, 160.927000f,  113.104000f, 86.066800f, 220.154000f,  43.628100f, 176.529000f, 129.719000f,
            //154.385000f, 94.200300f, 228.616000f,  212.179000f, 155.979000f, 220.537000f,  195.667000f, 70.741300f, 204.025000f,  121.361000f, 85.710300f, 137.130000f,
            //129.617000f, 193.041000f, 233.499000f,  88.138200f, 234.717000f, 154.488000f,  113.104000f, 76.287000f, 195.769000f,  41.829900f, 168.273000f, 146.231000f,
            //63.566900f, 209.634000f, 212.307000f,  213.963000f, 102.223000f, 237.050000f,  43.318400f, 160.016000f, 162.744000f,  146.129000f, 110.479000f, 147.839000f,
            //104.848000f, 160.016000f, 141.000000f,  113.104000f, 110.479000f, 136.671000f,  71.823100f, 220.084000f, 212.281000f,  113.104000f, 78.756700f, 204.025000f,
            //55.310600f, 184.785000f, 114.739000f,  47.054400f, 201.298000f, 137.439000f,  178.394000f, 118.735000f, 162.744000f,  203.923000f, 160.016000f, 231.556000f,
            //55.310600f, 231.432000f, 154.488000f,  170.898000f, 168.273000f, 233.088000f,  113.104000f, 102.223000f, 239.715000f,  88.335600f, 242.579000f, 205.183000f,
            //88.742600f, 93.966500f, 129.918000f,  146.129000f, 209.554000f, 174.373000f,  113.104000f, 242.579000f, 166.236000f,  170.898000f, 168.273000f, 165.800000f,
            //81.584500f, 118.735000f, 237.050000f,  170.898000f, 101.826000f, 237.454000f,  162.642000f, 168.273000f, 234.904000f,  82.732900f, 93.966500f, 171.000000f,
            //161.429000f, 85.710300f, 129.719000f,  71.823100f, 130.063000f, 104.950000f,  154.385000f, 209.554000f, 182.818000f,  121.361000f, 118.735000f, 249.694000f,
            //232.598000f, 85.710300f, 212.281000f,  63.566900f, 176.529000f, 223.111000f,  220.435000f, 102.223000f, 237.688000f,  155.518000f, 168.273000f, 146.231000f,
            //157.235000f, 143.504000f, 245.306000f,  88.335600f, 88.526400f, 171.000000f,  88.335600f, 193.041000f, 141.801000f,  96.591800f, 86.236400f, 212.281000f,
            //170.898000f, 60.941500f, 175.332000f,  129.617000f, 160.503000f, 139.154000f,  187.410000f, 126.991000f, 172.027000f,  228.692000f, 69.197800f, 166.432000f,
            //54.605800f, 184.785000f, 213.080000f,  137.873000f, 193.041000f, 231.873000f,  88.335600f, 160.016000f, 242.233000f,  154.385000f, 61.290000f, 169.492000f,
            //88.335600f, 263.127000f, 171.000000f,  71.823100f, 112.031000f, 220.537000f,  184.435000f, 77.454000f, 137.975000f,  78.420400f, 160.016000f, 237.861000f,
            //38.798200f, 173.048000f, 162.744000f,  129.617000f, 113.528000f, 245.306000f,  137.873000f, 77.454000f, 125.935000f,  71.823100f, 107.479000f, 187.512000f,
            //45.867100f, 168.273000f, 195.769000f,  231.209000f, 93.966500f, 220.537000f,  179.154000f, 143.504000f, 238.855000f,  76.434700f, 209.554000f, 146.231000f,
            //113.104000f, 217.810000f, 222.494000f,  60.703500f, 143.504000f, 220.537000f,  154.385000f, 208.171000f, 179.256000f,  224.119000f, 135.248000f, 228.794000f,
            //171.115000f, 69.240300f, 195.685000f,  156.575000f, 209.554000f, 204.025000f,  221.217000f, 135.248000f, 237.789000f,  204.208000f, 68.422800f, 146.066000f,
            //146.129000f, 201.298000f, 165.678000f,  121.361000f, 206.681000f, 146.231000f,  47.054400f, 160.016000f, 101.314000f,  104.848000f, 143.504000f, 147.585000f,
            //198.986000f, 118.735000f, 179.256000f,  154.385000f, 162.781000f, 154.488000f,  46.607300f, 160.016000f, 187.512000f,  162.642000f, 79.293800f, 212.281000f,
            //179.154000f, 188.125000f, 212.281000f,  96.591800f, 85.710300f, 210.461000f,  146.129000f, 96.438800f, 137.975000f,  38.940800f, 176.529000f, 154.488000f,
            //80.079300f, 143.504000f, 239.033000f,  43.085100f, 160.016000f, 154.488000f,  121.361000f, 202.816000f, 228.794000f,  187.410000f, 77.454000f, 140.718000f,
            //212.225000f, 77.476000f, 154.468000f,  146.129000f, 206.954000f, 171.000000f,  113.104000f, 220.515000f, 220.537000f,  79.716800f, 126.991000f, 237.334000f,
            //195.667000f, 126.991000f, 238.589000f,  220.435000f, 74.263000f, 212.281000f,  179.154000f, 58.497100f, 171.000000f,  137.873000f, 227.996000f, 179.256000f,
            //71.823100f, 135.248000f, 101.702000f,  113.104000f, 162.049000f, 146.231000f,  179.154000f, 126.991000f, 165.823000f,  138.952000f, 226.066000f, 179.256000f,
            //88.335600f, 231.559000f, 212.281000f,  195.667000f, 84.471900f, 154.488000f,  44.661100f, 209.554000f, 187.512000f,  96.591800f, 177.162000f, 237.270000f,
            //80.079300f, 163.071000f, 113.206000f,  203.923000f, 93.966500f, 166.040000f,  84.828600f, 201.298000f, 146.231000f,  154.385000f, 76.433300f, 204.025000f,
            //80.079300f, 193.041000f, 229.301000f,  137.873000f, 104.396000f, 237.050000f,  146.129000f, 118.735000f, 241.844000f,  88.335600f, 113.345000f, 137.975000f,
            //186.203000f, 126.991000f, 171.000000f,  228.692000f, 93.966500f, 215.707000f,  76.028300f, 135.248000f, 104.950000f,  119.610000f, 217.810000f, 220.537000f,
            //80.079300f, 201.298000f, 225.467000f,  162.642000f, 160.016000f, 238.372000f,  71.823100f, 143.504000f, 233.298000f,  129.617000f, 77.454000f, 136.454000f,
            //162.642000f, 63.745700f, 179.256000f,  184.007000f, 151.760000f, 237.050000f,  44.121500f, 184.785000f, 195.769000f,  48.488900f, 151.760000f, 187.512000f,
            //69.387200f, 110.479000f, 154.488000f,  57.295000f, 242.579000f, 179.256000f,  59.714800f, 151.760000f, 220.537000f,  212.468000f, 102.223000f, 187.512000f,
            //121.361000f, 217.320000f, 220.106000f,  221.315000f, 118.735000f, 237.223000f,  170.898000f, 190.021000f, 171.000000f,  104.848000f, 80.817500f, 204.025000f,
            //64.818000f, 160.016000f, 227.191000f,  59.503100f, 242.579000f, 162.744000f,  104.848000f, 118.735000f, 247.440000f,  110.260000f, 234.322000f, 162.744000f,
            //104.848000f, 251.502000f, 196.485000f,  162.642000f, 151.760000f, 162.460000f,  154.385000f, 189.992000f, 146.231000f,  236.948000f, 70.106800f, 195.769000f,
            //129.617000f, 209.554000f, 160.767000f,  104.848000f, 261.380000f, 179.256000f,  47.054400f, 209.554000f, 144.056000f,  162.642000f, 207.652000f, 179.256000f,
            //146.129000f, 118.735000f, 152.488000f,  146.129000f, 143.504000f, 159.554000f,  220.435000f, 79.813600f, 162.744000f,  162.642000f, 68.713500f, 129.236000f,
            //162.642000f, 195.028000f, 220.537000f,  55.310600f, 132.420000f, 121.463000f,  129.617000f, 234.322000f, 200.400000f,  137.873000f, 65.908700f, 154.488000f,
            //187.465000f, 143.089000f, 237.038000f,  228.094000f, 85.440700f, 187.512000f,  80.079300f, 118.735000f, 131.645000f,  212.179000f, 102.439000f, 186.969000f,
            //104.848000f, 160.016000f, 245.885000f,  103.615000f, 77.454000f, 171.000000f,  80.079300f, 100.522000f, 220.537000f,  88.335600f, 88.519000f, 187.512000f,
            //187.410000f, 77.454000f, 214.864000f,  113.104000f, 201.298000f, 230.400000f,  63.821700f, 119.138000f, 171.000000f,  179.154000f, 92.606800f, 228.794000f,
            //96.591800f, 93.966500f, 126.980000f,  187.410000f, 59.158600f, 179.256000f,  63.566900f, 118.735000f, 208.450000f,  96.591800f, 126.991000f, 249.283000f,
            //162.642000f, 62.144000f, 137.975000f,  212.179000f, 110.479000f, 236.710000f,  141.772000f, 110.479000f, 146.231000f,  145.567000f, 160.092000f, 154.488000f,
            //55.310600f, 238.312000f, 187.512000f,  163.024000f, 176.529000f, 145.682000f,  71.823100f, 107.810000f, 179.256000f,  113.104000f, 209.554000f, 226.657000f,
            //88.335600f, 126.991000f, 135.683000f,  162.642000f, 187.097000f, 154.488000f,  187.410000f, 90.724700f, 154.488000f,  154.385000f, 151.760000f, 161.355000f,
            //154.385000f, 59.513800f, 154.488000f,  51.004000f, 143.504000f, 129.719000f,  104.848000f, 234.322000f, 162.304000f,  212.179000f, 160.016000f, 223.047000f,
            //71.823100f, 250.835000f, 192.945000f,  43.344500f, 201.298000f, 146.231000f,  71.823100f, 151.760000f, 234.537000f,  113.104000f, 118.735000f, 248.849000f,
            //42.835300f, 209.554000f, 179.256000f,  162.642000f, 71.123500f, 195.769000f,  146.129000f, 107.320000f, 146.231000f,  88.335600f, 89.056000f, 137.975000f,
            //49.953200f, 201.298000f, 204.025000f,  146.129000f, 151.760000f, 160.483000f,  146.129000f, 195.793000f, 154.488000f,  63.566900f, 126.991000f, 101.676000f,
            //129.617000f, 80.779000f, 212.281000f,  55.310600f, 133.990000f, 137.975000f,  130.447000f, 69.273100f, 171.000000f,  129.617000f, 186.060000f, 237.050000f,
            //77.116700f, 143.504000f, 237.050000f,  179.154000f, 69.197800f, 198.685000f,  162.642000f, 58.124400f, 162.744000f,  234.859000f, 93.966500f, 228.794000f,
            //64.174100f, 126.991000f, 219.756000f,  50.065600f, 217.810000f, 146.231000f,  203.923000f, 126.991000f, 239.260000f,  179.154000f, 76.692400f, 212.281000f,
            //48.950900f, 176.529000f, 204.025000f,  80.079300f, 209.554000f, 148.488000f,  187.410000f, 61.303200f, 186.073000f,  212.179000f, 143.504000f, 239.475000f,
            //96.591800f, 82.712800f, 146.231000f,  96.591800f, 85.710300f, 134.577000f,  236.948000f, 76.603600f, 179.256000f,  195.667000f, 124.326000f, 179.256000f,
            //39.013600f, 200.663000f, 162.744000f,  170.898000f, 151.760000f, 240.306000f,  195.667000f, 156.589000f, 212.281000f,  104.848000f, 238.601000f, 162.744000f,
            //40.200300f, 168.273000f, 162.744000f,  113.104000f, 239.676000f, 204.025000f,  210.731000f, 85.051600f, 162.744000f,  71.823100f, 178.959000f, 228.794000f,
            //207.132000f, 60.941500f, 171.000000f,  223.000000f, 93.966500f, 204.025000f,  137.873000f, 118.735000f, 148.536000f,  80.079300f, 135.248000f, 109.990000f,
            //71.823100f, 179.594000f, 121.463000f,  125.735000f, 217.810000f, 162.744000f,  96.591800f, 168.273000f, 241.497000f,  217.782000f, 118.735000f, 212.281000f,
            //71.823100f, 110.479000f, 137.327000f,  93.847700f, 176.529000f, 237.050000f,  129.617000f, 85.710300f, 218.667000f,  80.079300f, 261.522000f, 171.000000f,
            //217.902000f, 93.966500f, 187.512000f,  104.974000f, 259.148000f, 170.878000f,  181.151000f, 193.041000f, 195.769000f,  47.054400f, 221.560000f, 171.000000f,
            //195.667000f, 62.317600f, 187.512000f,  146.129000f, 68.701800f, 128.787000f,  228.692000f, 82.408300f, 228.794000f,  88.335600f, 259.091000f, 159.988000f,
            //129.617000f, 200.839000f, 228.469000f,  195.667000f, 135.248000f, 238.442000f,  195.667000f, 144.973000f, 237.050000f,  71.823100f, 118.735000f, 225.490000f,
            //187.410000f, 81.673500f, 220.537000f,  148.370000f, 209.554000f, 212.281000f,  88.335600f, 124.017000f, 137.975000f,  162.642000f, 77.454000f, 208.804000f,
            //150.266000f, 60.941500f, 162.744000f,  187.410000f, 118.735000f, 168.697000f,  154.385000f, 201.298000f, 167.728000f,  113.104000f, 72.965300f, 154.488000f,
            //187.695000f, 168.085000f, 187.512000f,  68.615100f, 110.479000f, 204.025000f,  115.018000f, 168.273000f, 137.975000f,  212.179000f, 66.432000f, 195.769000f,
            //113.104000f, 72.878200f, 162.744000f,  88.534100f, 126.991000f, 245.176000f,  220.435000f, 64.438700f, 171.000000f,  137.873000f, 66.400100f, 146.231000f,
            //170.898000f, 59.791100f, 171.000000f,  88.335600f, 209.554000f, 225.244000f,  179.154000f, 93.966500f, 229.893000f,  121.361000f, 217.810000f, 160.042000f,
            //129.617000f, 139.247000f, 154.488000f,  59.004800f, 126.991000f, 187.512000f,  96.591800f, 259.259000f, 162.343000f,  154.385000f, 63.519600f, 137.975000f,
            //212.179000f, 63.301800f, 187.512000f,  80.079300f, 97.304600f, 179.256000f,  129.617000f, 196.910000f, 129.719000f,  162.642000f, 104.128000f, 146.231000f,
            //171.029000f, 176.529000f, 162.390000f,  84.221300f, 102.223000f, 228.794000f,  71.823100f, 124.791000f, 113.206000f,  71.823100f, 162.920000f, 104.950000f,
            //155.636000f, 60.941600f, 146.231000f,  220.260000f, 93.966500f, 196.443000f,  96.591800f, 184.785000f, 234.483000f,  113.104000f, 151.760000f, 157.567000f,
            //129.617000f, 70.254400f, 179.256000f,  174.924000f, 193.041000f, 212.281000f,  195.667000f, 102.223000f, 165.535000f,  125.599000f, 242.579000f, 195.769000f,
            //121.361000f, 168.273000f, 132.456000f,  183.564000f, 160.016000f, 171.000000f,  88.335600f, 89.145500f, 204.025000f,  214.652000f, 151.760000f, 220.537000f,
            //41.776200f, 193.041000f, 146.231000f,  170.898000f, 86.788300f, 137.975000f,  179.154000f, 185.545000f, 179.256000f,  129.617000f, 118.735000f, 248.051000f,
            //170.898000f, 190.462000f, 220.537000f,  53.181100f, 234.322000f, 171.000000f,  195.667000f, 69.197800f, 201.453000f,  146.129000f, 81.800500f, 212.281000f,
            //212.179000f, 135.248000f, 206.115000f,  224.866000f, 69.197800f, 162.744000f,  174.613000f, 60.941500f, 179.256000f,  58.466600f, 242.579000f, 187.512000f,
            //80.079300f, 151.760000f, 239.029000f,  137.873000f, 229.891000f, 195.769000f,  45.459800f, 168.273000f, 113.206000f,  220.877000f, 143.504000f, 237.253000f,
            //121.361000f, 201.298000f, 229.761000f,  154.385000f, 126.991000f, 157.088000f,  129.911000f, 119.092000f, 146.137000f,  91.018100f, 143.504000f, 129.719000f,
            //188.061000f, 143.504000f, 179.256000f,  162.642000f, 69.197800f, 192.274000f,  174.084000f, 176.529000f, 228.794000f,  184.217000f, 184.785000f, 195.769000f,
            //56.919900f, 226.066000f, 146.231000f,  121.361000f, 143.504000f, 250.170000f,  185.678000f, 168.273000f, 179.256000f,  179.154000f, 182.038000f, 220.537000f,
            //54.530300f, 210.067000f, 137.975000f,  187.443000f, 151.780000f, 195.691000f,  179.154000f, 77.454000f, 213.753000f,  154.385000f, 64.494200f, 179.256000f,
            //146.129000f, 77.235100f, 204.606000f,  72.391000f, 217.810000f, 145.894000f,  104.848000f, 86.732900f, 220.537000f,  45.662600f, 217.810000f, 162.744000f,
            //96.261000f, 160.016000f, 130.073000f,  170.247000f, 77.454000f, 130.067000f,  146.129000f, 191.477000f, 137.975000f,  76.120900f, 102.223000f, 146.231000f,
            //154.143000f, 176.529000f, 138.162000f,  113.104000f, 226.066000f, 215.642000f,  40.741600f, 201.298000f, 179.256000f,  212.179000f, 85.710300f, 229.239000f,
            //137.873000f, 176.529000f, 239.950000f,  137.873000f, 197.491000f, 228.794000f,  162.642000f, 206.499000f, 204.025000f,  45.541900f, 201.298000f, 195.769000f,
            //71.823100f, 257.838000f, 179.256000f,  104.848000f, 78.936400f, 195.769000f,  80.079300f, 96.217100f, 204.025000f,  162.642000f, 102.676000f, 236.752000f,
            //213.521000f, 143.504000f, 212.281000f,  124.667000f, 110.479000f, 245.306000f,  80.079300f, 97.456200f, 162.744000f,  51.954300f, 135.248000f, 113.206000f,
            //154.385000f, 190.407000f, 228.794000f, 146.129000f, 64.644000f, 171.000000f };

            //void main(int /*argc*/,const char * /*argv*/)
            //{

            //  REAL matrix[16];
            //  REAL sides[3];
            //  unsigned int PCOUNT = sizeof(points)/(sizeof(REAL)*3);
            //  bf_computeBestFitOBB(PCOUNT,points,sizeof(REAL)*3,sides,matrix,true);

            //  printf("Best Fit OBB dimensions: %0.9f,%0.9f,%0.9f\r\n", sides[0], sides[1], sides[2] );
            //  printf("Best Fit OBB matrix is:\r\n");
            //  printf("Row1:  %0.9f, %09f, %0.9f, %0.9f \r\n", matrix[0], matrix[1], matrix[2], matrix[3] );
            //  printf("Row2:  %0.9f, %09f, %0.9f, %0.9f \r\n", matrix[4], matrix[5], matrix[6], matrix[7] );
            //  printf("Row3:  %0.9f, %09f, %0.9f, %0.9f \r\n", matrix[8], matrix[9], matrix[10], matrix[11] );
            //  printf("Row4:  %0.9f, %09f, %0.9f, %0.9f \r\n", matrix[12], matrix[13], matrix[14], matrix[15] );


            //}

            //#endif

            #endregion
            #region Converted C#

            #region Declaration Section

            //#include <string.h>
            //#include <stdio.h>
            //#include <stdlib.h>
            //#include <math.h>
            //#include <float.h>

            //typedef unsigned int NxU32;
            //typedef int NxI32;
            //typedef float REAL;

            private const double BF_DEG_TO_RAD = ((2d * Math.PI) / 360d);
            private const double BF_RAD_TO_DEG = (360d / (2d * Math.PI));

            #endregion

            #region bf_getTranslation

            void bf_getTranslation(double[] matrix, double[] t)
            {
                t[0] = matrix[3 * 4 + 0];
                t[1] = matrix[3 * 4 + 1];
                t[2] = matrix[3 * 4 + 2];
            }

            #endregion
            #region bf_matrixToQuat

            //void bf_matrixToQuat(const double *matrix,double *quat) // convert the 3x3 portion of a 4x4 matrix into a quaterion as x,y,z,w
            //{

            //  double tr = matrix[0*4+0] + matrix[1*4+1] + matrix[2*4+2];

            //  // check the diagonal

            //  if (tr > 0d )
            //  {
            //    double s = (double) sqrt ( (double) (tr + 1d) );
            //    quat[3] = s * 0.5f;
            //    s = 0.5f / s;
            //    quat[0] = (matrix[1*4+2] - matrix[2*4+1]) * s;
            //    quat[1] = (matrix[2*4+0] - matrix[0*4+2]) * s;
            //    quat[2] = (matrix[0*4+1] - matrix[1*4+0]) * s;

            //  }
            //  else
            //  {
            //    // diagonal is negative
            //    NxI32 nxt[3] = {1, 2, 0};
            //    double  qa[4];

            //    NxI32 i = 0;

            //    if (matrix[1*4+1] > matrix[0*4+0]) i = 1;
            //    if (matrix[2*4+2] > matrix[i*4+i]) i = 2;

            //    NxI32 j = nxt[i];
            //    NxI32 k = nxt[j];

            //    double s = sqrt ( ((matrix[i*4+i] - (matrix[j*4+j] + matrix[k*4+k])) + 1d) );

            //    qa[i] = s * 0.5f;

            //    if (s != 0d ) s = 0.5f / s;

            //    qa[3] = (matrix[j*4+k] - matrix[k*4+j]) * s;
            //    qa[j] = (matrix[i*4+j] + matrix[j*4+i]) * s;
            //    qa[k] = (matrix[i*4+k] + matrix[k*4+i]) * s;

            //    quat[0] = qa[0];
            //    quat[1] = qa[1];
            //    quat[2] = qa[2];
            //    quat[3] = qa[3];
            //  }


            //}

            #endregion
            #region bf_matrixMultiply

            //void  bf_matrixMultiply(const double *pA,const double *pB,double *pM)
            //{
            //  double a = pA[0*4+0] * pB[0*4+0] + pA[0*4+1] * pB[1*4+0] + pA[0*4+2] * pB[2*4+0] + pA[0*4+3] * pB[3*4+0];
            //  double b = pA[0*4+0] * pB[0*4+1] + pA[0*4+1] * pB[1*4+1] + pA[0*4+2] * pB[2*4+1] + pA[0*4+3] * pB[3*4+1];
            //  double c = pA[0*4+0] * pB[0*4+2] + pA[0*4+1] * pB[1*4+2] + pA[0*4+2] * pB[2*4+2] + pA[0*4+3] * pB[3*4+2];
            //  double d = pA[0*4+0] * pB[0*4+3] + pA[0*4+1] * pB[1*4+3] + pA[0*4+2] * pB[2*4+3] + pA[0*4+3] * pB[3*4+3];

            //  double e = pA[1*4+0] * pB[0*4+0] + pA[1*4+1] * pB[1*4+0] + pA[1*4+2] * pB[2*4+0] + pA[1*4+3] * pB[3*4+0];
            //  double f = pA[1*4+0] * pB[0*4+1] + pA[1*4+1] * pB[1*4+1] + pA[1*4+2] * pB[2*4+1] + pA[1*4+3] * pB[3*4+1];
            //  double g = pA[1*4+0] * pB[0*4+2] + pA[1*4+1] * pB[1*4+2] + pA[1*4+2] * pB[2*4+2] + pA[1*4+3] * pB[3*4+2];
            //  double h = pA[1*4+0] * pB[0*4+3] + pA[1*4+1] * pB[1*4+3] + pA[1*4+2] * pB[2*4+3] + pA[1*4+3] * pB[3*4+3];

            //  double i = pA[2*4+0] * pB[0*4+0] + pA[2*4+1] * pB[1*4+0] + pA[2*4+2] * pB[2*4+0] + pA[2*4+3] * pB[3*4+0];
            //  double j = pA[2*4+0] * pB[0*4+1] + pA[2*4+1] * pB[1*4+1] + pA[2*4+2] * pB[2*4+1] + pA[2*4+3] * pB[3*4+1];
            //  double k = pA[2*4+0] * pB[0*4+2] + pA[2*4+1] * pB[1*4+2] + pA[2*4+2] * pB[2*4+2] + pA[2*4+3] * pB[3*4+2];
            //  double l = pA[2*4+0] * pB[0*4+3] + pA[2*4+1] * pB[1*4+3] + pA[2*4+2] * pB[2*4+3] + pA[2*4+3] * pB[3*4+3];

            //  double m = pA[3*4+0] * pB[0*4+0] + pA[3*4+1] * pB[1*4+0] + pA[3*4+2] * pB[2*4+0] + pA[3*4+3] * pB[3*4+0];
            //  double n = pA[3*4+0] * pB[0*4+1] + pA[3*4+1] * pB[1*4+1] + pA[3*4+2] * pB[2*4+1] + pA[3*4+3] * pB[3*4+1];
            //  double o = pA[3*4+0] * pB[0*4+2] + pA[3*4+1] * pB[1*4+2] + pA[3*4+2] * pB[2*4+2] + pA[3*4+3] * pB[3*4+2];
            //  double p = pA[3*4+0] * pB[0*4+3] + pA[3*4+1] * pB[1*4+3] + pA[3*4+2] * pB[2*4+3] + pA[3*4+3] * pB[3*4+3];

            //  pM[0] = a;
            //  pM[1] = b;
            //  pM[2] = c;
            //  pM[3] = d;

            //  pM[4] = e;
            //  pM[5] = f;
            //  pM[6] = g;
            //  pM[7] = h;

            //  pM[8] = i;
            //  pM[9] = j;
            //  pM[10] = k;
            //  pM[11] = l;

            //  pM[12] = m;
            //  pM[13] = n;
            //  pM[14] = o;
            //  pM[15] = p;
            //}

            #endregion
            #region bf_eulerToQuat

            //void bf_eulerToQuat(double roll,double pitch,double yaw,double *quat) // convert euler angles to quaternion.
            //{
            //  roll  *= 0.5f;
            //  pitch *= 0.5f;
            //  yaw   *= 0.5f;

            //  double cr = cos(roll);
            //  double cp = cos(pitch);
            //  double cy = cos(yaw);

            //  double sr = sin(roll);
            //  double sp = sin(pitch);
            //  double sy = sin(yaw);

            //  double cpcy = cp * cy;
            //  double spsy = sp * sy;
            //  double spcy = sp * cy;
            //  double cpsy = cp * sy;

            //  quat[0]   = ( sr * cpcy - cr * spsy);
            //  quat[1]   = ( cr * spcy + sr * cpsy);
            //  quat[2]   = ( cr * cpsy - sr * spcy);
            //  quat[3]   = cr * cpcy + sr * spsy;
            //}

            #endregion
            #region bf_eulerToQuat

            //void  bf_eulerToQuat(const double *euler,double *quat) // convert euler angles to quaternion.
            //{
            //  bf_eulerToQuat(euler[0],euler[1],euler[2],quat);
            //}

            #endregion
            #region bf_setTranslation

            //void  bf_setTranslation(const double *translation,double *matrix)
            //{
            //  matrix[12] = translation[0];
            //  matrix[13] = translation[1];
            //  matrix[14] = translation[2];
            //}

            #endregion
            #region bf_transform

            //void  bf_transform(const double matrix[16],const double v[3],double t[3]) // rotate and translate this point
            //{
            //  if ( matrix )
            //  {
            //    double tx = (matrix[0*4+0] * v[0]) +  (matrix[1*4+0] * v[1]) + (matrix[2*4+0] * v[2]) + matrix[3*4+0];
            //    double ty = (matrix[0*4+1] * v[0]) +  (matrix[1*4+1] * v[1]) + (matrix[2*4+1] * v[2]) + matrix[3*4+1];
            //    double tz = (matrix[0*4+2] * v[0]) +  (matrix[1*4+2] * v[1]) + (matrix[2*4+2] * v[2]) + matrix[3*4+2];
            //    t[0] = tx;
            //    t[1] = ty;
            //    t[2] = tz;
            //  }
            //  else
            //  {
            //    t[0] = v[0];
            //    t[1] = v[1];
            //    t[2] = v[2];
            //  }
            //}

            #endregion
            #region bf_dot

            //double bf_dot(const double *p1,const double *p2)
            //{
            //  return p1[0]*p2[0]+p1[1]*p2[1]+p1[2]*p2[2];
            //}

            #endregion
            #region bf_cross

            //void bf_cross(double *cross,const double *a,const double *b)
            //{
            //  cross[0] = a[1]*b[2] - a[2]*b[1];
            //  cross[1] = a[2]*b[0] - a[0]*b[2];
            //  cross[2] = a[0]*b[1] - a[1]*b[0];
            //}

            #endregion
            #region bf_quatToMatrix

            //void bf_quatToMatrix(const double *quat,double *matrix) // convert quaterinion rotation to matrix, zeros out the translation component.
            //{
            //  double xx = quat[0]*quat[0];
            //  double yy = quat[1]*quat[1];
            //  double zz = quat[2]*quat[2];
            //  double xy = quat[0]*quat[1];
            //  double xz = quat[0]*quat[2];
            //  double yz = quat[1]*quat[2];
            //  double wx = quat[3]*quat[0];
            //  double wy = quat[3]*quat[1];
            //  double wz = quat[3]*quat[2];

            //  matrix[0*4+0] = 1 - 2 * ( yy + zz );
            //  matrix[1*4+0] =     2 * ( xy - wz );
            //  matrix[2*4+0] =     2 * ( xz + wy );

            //  matrix[0*4+1] =     2 * ( xy + wz );
            //  matrix[1*4+1] = 1 - 2 * ( xx + zz );
            //  matrix[2*4+1] =     2 * ( yz - wx );

            //  matrix[0*4+2] =     2 * ( xz - wy );
            //  matrix[1*4+2] =     2 * ( yz + wx );
            //  matrix[2*4+2] = 1 - 2 * ( xx + yy );

            //  matrix[3*4+0] = matrix[3*4+1] = matrix[3*4+2] = (double) 0d;
            //  matrix[0*4+3] = matrix[1*4+3] = matrix[2*4+3] = (double) 0d;
            //  matrix[3*4+3] =(double) 1d;
            //}

            #endregion
            #region bf_rotationArc

            // Reference, from Stan Melax in Game Gems I
            //  Quaternion q;
            //  vector3 c = CrossProduct(v0,v1);
            //  double   d = DotProduct(v0,v1);
            //  double   s = (double)sqrt((1+d)*2);
            //  q.x = c.x / s;
            //  q.y = c.y / s;
            //  q.z = c.z / s;
            //  q.w = s /2d;
            //  return q;
            //void bf_rotationArc(const double *v0,const double *v1,double *quat)
            //{
            //  double cross[3];

            //  bf_cross(cross,v0,v1);
            //  double d = bf_dot(v0,v1);
            //  double s = sqrt((1+d)*2);
            //  double recip = 1d / s;

            //  quat[0] = cross[0] * recip;
            //  quat[1] = cross[1] * recip;
            //  quat[2] = cross[2] * recip;
            //  quat[3] = s * 0.5f;
            //}

            #endregion
            #region bf_planeToMatrix

            //void bf_planeToMatrix(const double *plane,double *matrix) // convert a plane equation to a 4x4 rotation matrix
            //{
            //  double ref[3] = { 0, 1, 0 };
            //  double quat[4];
            //  bf_rotationArc(ref,plane,quat);
            //  bf_quatToMatrix(quat,matrix);
            //  double origin[3] = { 0, -plane[3], 0 };
            //  double center[3];
            //  bf_transform(matrix,origin,center);
            //  bf_setTranslation(center,matrix);
            //}

            #endregion
            #region bf_inverseRT

            //void bf_inverseRT(const double matrix[16],const double pos[3],double t[3]) // inverse rotate translate the point.
            //{

            //        double _x = pos[0] - matrix[3*4+0];
            //        double _y = pos[1] - matrix[3*4+1];
            //        double _z = pos[2] - matrix[3*4+2];

            //        // Multiply inverse-translated source vector by inverted rotation transform

            //        t[0] = (matrix[0*4+0] * _x) + (matrix[0*4+1] * _y) + (matrix[0*4+2] * _z);
            //        t[1] = (matrix[1*4+0] * _x) + (matrix[1*4+1] * _y) + (matrix[1*4+2] * _z);
            //        t[2] = (matrix[2*4+0] * _x) + (matrix[2*4+1] * _y) + (matrix[2*4+2] * _z);

            //}

            #endregion
            #region bf_rotate

            //void  bf_rotate(const double matrix[16],const double v[3],double t[3]) // rotate and translate this point
            //{
            //  if ( matrix )
            //  {
            //    double tx = (matrix[0*4+0] * v[0]) +  (matrix[1*4+0] * v[1]) + (matrix[2*4+0] * v[2]);
            //    double ty = (matrix[0*4+1] * v[0]) +  (matrix[1*4+1] * v[1]) + (matrix[2*4+1] * v[2]);
            //    double tz = (matrix[0*4+2] * v[0]) +  (matrix[1*4+2] * v[1]) + (matrix[2*4+2] * v[2]);
            //    t[0] = tx;
            //    t[1] = ty;
            //    t[2] = tz;
            //  }
            //  else
            //  {
            //    t[0] = v[0];
            //    t[1] = v[1];
            //    t[2] = v[2];
            //  }
            //}

            #endregion
            #region bf_computeOBB

            // computes the OBB for this set of points relative to this transform matrix.
            //void bf_computeOBB(NxU32 vcount,const double *points,NxU32 pstride,double *sides,double *matrix)
            //{
            //  const char *src = (const char *) points;

            //  double bmin[3] = { 1e9, 1e9, 1e9 };
            //  double bmax[3] = { -1e9, -1e9, -1e9 };

            //  for (NxU32 i=0; i<vcount; i++)
            //  {
            //    const double *p = (const double *) src;
            //    double t[3];

            //    bf_inverseRT(matrix, p, t ); // inverse rotate translate

            //    if ( t[0] < bmin[0] ) bmin[0] = t[0];
            //    if ( t[1] < bmin[1] ) bmin[1] = t[1];
            //    if ( t[2] < bmin[2] ) bmin[2] = t[2];

            //    if ( t[0] > bmax[0] ) bmax[0] = t[0];
            //    if ( t[1] > bmax[1] ) bmax[1] = t[1];
            //    if ( t[2] > bmax[2] ) bmax[2] = t[2];

            //    src+=pstride;
            //  }

            //  double center[3];

            //  sides[0] = bmax[0]-bmin[0];
            //  sides[1] = bmax[1]-bmin[1];
            //  sides[2] = bmax[2]-bmin[2];

            //  center[0] = sides[0]*0.5f+bmin[0];
            //  center[1] = sides[1]*0.5f+bmin[1];
            //  center[2] = sides[2]*0.5f+bmin[2];

            //  double ocenter[3];

            //  bf_rotate(matrix,center,ocenter);

            //  matrix[12]+=ocenter[0];
            //  matrix[13]+=ocenter[1];
            //  matrix[14]+=ocenter[2];

            //}

            #endregion

            #region Class: Eigen

            //template <class Type> class Eigen
            //{
            //public:
            //  void DecrSortEigenStuff(void)
            //  {
            //    Tridiagonal(); //diagonalize the matrix.
            //    QLAlgorithm(); //
            //    DecreasingSort();
            //    GuaranteeRotation();
            //  }

            //  void Tridiagonal(void)
            //  {
            //    Type fM00 = mElement[0][0];
            //    Type fM01 = mElement[0][1];
            //    Type fM02 = mElement[0][2];
            //    Type fM11 = mElement[1][1];
            //    Type fM12 = mElement[1][2];
            //    Type fM22 = mElement[2][2];

            //    m_afDiag[0] = fM00;
            //    m_afSubd[2] = 0;
            //    if (fM02 != (Type)0.0)
            //    {
            //      Type fLength = sqrt(fM01*fM01+fM02*fM02);
            //      Type fInvLength = ((Type)1.0)/fLength;
            //      fM01 *= fInvLength;
            //      fM02 *= fInvLength;
            //      Type fQ = ((Type)2.0)*fM01*fM12+fM02*(fM22-fM11);
            //      m_afDiag[1] = fM11+fM02*fQ;
            //      m_afDiag[2] = fM22-fM02*fQ;
            //      m_afSubd[0] = fLength;
            //      m_afSubd[1] = fM12-fM01*fQ;
            //      mElement[0][0] = (Type)1.0;
            //      mElement[0][1] = (Type)0.0;
            //      mElement[0][2] = (Type)0.0;
            //      mElement[1][0] = (Type)0.0;
            //      mElement[1][1] = fM01;
            //      mElement[1][2] = fM02;
            //      mElement[2][0] = (Type)0.0;
            //      mElement[2][1] = fM02;
            //      mElement[2][2] = -fM01;
            //      m_bIsRotation = false;
            //    }
            //    else
            //    {
            //      m_afDiag[1] = fM11;
            //      m_afDiag[2] = fM22;
            //      m_afSubd[0] = fM01;
            //      m_afSubd[1] = fM12;
            //      mElement[0][0] = (Type)1.0;
            //      mElement[0][1] = (Type)0.0;
            //      mElement[0][2] = (Type)0.0;
            //      mElement[1][0] = (Type)0.0;
            //      mElement[1][1] = (Type)1.0;
            //      mElement[1][2] = (Type)0.0;
            //      mElement[2][0] = (Type)0.0;
            //      mElement[2][1] = (Type)0.0;
            //      mElement[2][2] = (Type)1.0;
            //      m_bIsRotation = true;
            //    }
            //  }

            //  bool QLAlgorithm(void)
            //  {
            //    const NxI32 iMaxIter = 32;

            //    for (NxI32 i0 = 0; i0 <3; i0++)
            //    {
            //      NxI32 i1;
            //      for (i1 = 0; i1 < iMaxIter; i1++)
            //      {
            //        NxI32 i2;
            //        for (i2 = i0; i2 <= (3-2); i2++)
            //        {
            //          Type fTmp = fabs(m_afDiag[i2]) + fabs(m_afDiag[i2+1]);
            //          if ( fabs(m_afSubd[i2]) + fTmp == fTmp )
            //            break;
            //        }
            //        if (i2 == i0)
            //        {
            //          break;
            //        }

            //        Type fG = (m_afDiag[i0+1] - m_afDiag[i0])/(((Type)2.0) * m_afSubd[i0]);
            //        Type fR = sqrt(fG*fG+(Type)1.0);
            //        if (fG < (Type)0.0)
            //        {
            //          fG = m_afDiag[i2]-m_afDiag[i0]+m_afSubd[i0]/(fG-fR);
            //        }
            //        else
            //        {
            //          fG = m_afDiag[i2]-m_afDiag[i0]+m_afSubd[i0]/(fG+fR);
            //        }
            //        Type fSin = (Type)1.0, fCos = (Type)1.0, fP = (Type)0.0;
            //        for (NxI32 i3 = i2-1; i3 >= i0; i3--)
            //        {
            //          Type fF = fSin*m_afSubd[i3];
            //          Type fB = fCos*m_afSubd[i3];
            //          if (fabs(fF) >= fabs(fG))
            //          {
            //            fCos = fG/fF;
            //            fR = sqrt(fCos*fCos+(Type)1.0);
            //            m_afSubd[i3+1] = fF*fR;
            //            fSin = ((Type)1.0)/fR;
            //            fCos *= fSin;
            //          }
            //          else
            //          {
            //            fSin = fF/fG;
            //            fR = sqrt(fSin*fSin+(Type)1.0);
            //            m_afSubd[i3+1] = fG*fR;
            //            fCos = ((Type)1.0)/fR;
            //            fSin *= fCos;
            //          }
            //          fG = m_afDiag[i3+1]-fP;
            //          fR = (m_afDiag[i3]-fG)*fSin+((Type)2.0)*fB*fCos;
            //          fP = fSin*fR;
            //          m_afDiag[i3+1] = fG+fP;
            //          fG = fCos*fR-fB;
            //          for (NxI32 i4 = 0; i4 < 3; i4++)
            //          {
            //            fF = mElement[i4][i3+1];
            //            mElement[i4][i3+1] = fSin*mElement[i4][i3]+fCos*fF;
            //            mElement[i4][i3] = fCos*mElement[i4][i3]-fSin*fF;
            //          }
            //        }
            //        m_afDiag[i0] -= fP;
            //        m_afSubd[i0] = fG;
            //        m_afSubd[i2] = (Type)0.0;
            //      }
            //      if (i1 == iMaxIter)
            //      {
            //        return false;
            //      }
            //    }
            //    return true;
            //  }

            //  void DecreasingSort(void)
            //  {
            //    //sort eigenvalues in decreasing order, e[0] >= ... >= e[iSize-1]
            //    for (NxI32 i0 = 0, i1; i0 <= 3-2; i0++)
            //    {
            //      // locate maximum eigenvalue
            //      i1 = i0;
            //      Type fMax = m_afDiag[i1];
            //      NxI32 i2;
            //      for (i2 = i0+1; i2 < 3; i2++)
            //      {
            //        if (m_afDiag[i2] > fMax)
            //        {
            //          i1 = i2;
            //          fMax = m_afDiag[i1];
            //        }
            //      }

            //      if (i1 != i0)
            //      {
            //        // swap eigenvalues
            //        m_afDiag[i1] = m_afDiag[i0];
            //        m_afDiag[i0] = fMax;
            //        // swap eigenvectors
            //        for (i2 = 0; i2 < 3; i2++)
            //        {
            //          Type fTmp = mElement[i2][i0];
            //          mElement[i2][i0] = mElement[i2][i1];
            //          mElement[i2][i1] = fTmp;
            //          m_bIsRotation = !m_bIsRotation;
            //        }
            //      }
            //    }
            //  }


            //  void GuaranteeRotation(void)
            //  {
            //    if (!m_bIsRotation)
            //    {
            //      // change sign on the first column
            //      for (NxI32 iRow = 0; iRow <3; iRow++)
            //      {
            //        mElement[iRow][0] = -mElement[iRow][0];
            //      }
            //    }
            //  }

            //  Type mElement[3][3];
            //  Type m_afDiag[3];
            //  Type m_afSubd[3];
            //  bool m_bIsRotation;
            //};

            #endregion

            #region bf_computeBestFitPlane

            public static bool bf_computeBestFitPlane(int vcount, double[] points, int vstride, /*double[] weights,*/ int wstride, double[] plane)
            {
                bool ret = false;

                double[] kOrigin = new[] { 0d, 0d, 0d };

                double wtotal = 0;


                //NOTE: I think the empty curly braces are for scoping


                //TODO: Figure out what this block is trying to do - this is weighting the points???.  Get rid of this
                //{       
                //char[] source = points;     // why char?  implicit rounding?
                //char[] wsource = weights;

                //    for (int i=0; i<vcount; i++)
                //    {
                //      double[] p = source;

                //      double w = 1;

                //      if ( wsource )        //wtf????
                //      {
                //        double[] ws = wsource;
                //        w = *ws; 
                //        wsource+=wstride;
                //      }

                //      kOrigin[0]+=p[0]*w;
                //      kOrigin[1]+=p[1]*w;
                //      kOrigin[2]+=p[2]*w;

                //      wtotal+=w;

                //      source+=vstride;
                //    }
                //}



                double recip = 1d / wtotal; // reciprocol of total weighting

                //  kOrigin[0]*=recip;
                //  kOrigin[1]*=recip;
                //  kOrigin[2]*=recip;


                //  double fSumXX=0;
                //  double fSumXY=0;
                //  double fSumXZ=0;

                //  double fSumYY=0;
                //  double fSumYZ=0;
                //  double fSumZZ=0;


                //  {
                //    const char *source  = (const char *) points;
                //    const char *wsource = (const char *) weights;

                //    for (NxU32 i=0; i<vcount; i++)
                //    {

                //      const double *p = (const double *) source;

                //      double w = 1;

                //      if ( wsource )
                //      {
                //        const double *ws = (const double *) wsource;
                //        w = *ws; //
                //        wsource+=wstride;
                //      }

                //      double kDiff[3];

                //      kDiff[0] = w*(p[0] - kOrigin[0]); // apply vertex weighting!
                //      kDiff[1] = w*(p[1] - kOrigin[1]);
                //      kDiff[2] = w*(p[2] - kOrigin[2]);

                //      fSumXX+= kDiff[0] * kDiff[0]; // sume of the squares of the differences.
                //      fSumXY+= kDiff[0] * kDiff[1]; // sume of the squares of the differences.
                //      fSumXZ+= kDiff[0] * kDiff[2]; // sume of the squares of the differences.

                //      fSumYY+= kDiff[1] * kDiff[1];
                //      fSumYZ+= kDiff[1] * kDiff[2];
                //      fSumZZ+= kDiff[2] * kDiff[2];


                //      source+=vstride;
                //    }
                //  }

                //  fSumXX *= recip;
                //  fSumXY *= recip;
                //  fSumXZ *= recip;
                //  fSumYY *= recip;
                //  fSumYZ *= recip;
                //  fSumZZ *= recip;

                //  // setup the eigensolver
                //  Eigen<double> kES;

                //  kES.mElement[0][0] = fSumXX;
                //  kES.mElement[0][1] = fSumXY;
                //  kES.mElement[0][2] = fSumXZ;

                //  kES.mElement[1][0] = fSumXY;
                //  kES.mElement[1][1] = fSumYY;
                //  kES.mElement[1][2] = fSumYZ;

                //  kES.mElement[2][0] = fSumXZ;
                //  kES.mElement[2][1] = fSumYZ;
                //  kES.mElement[2][2] = fSumZZ;

                //  // compute eigenstuff, smallest eigenvalue is in last position
                //  kES.DecrSortEigenStuff();

                //  double kNormal[3];

                //  kNormal[0] = kES.mElement[0][2];
                //  kNormal[1] = kES.mElement[1][2];
                //  kNormal[2] = kES.mElement[2][2];

                //  // the minimum energy
                //  plane[0] = kNormal[0];
                //  plane[1] = kNormal[1];
                //  plane[2] = kNormal[2];

                //  plane[3] = 0 - bf_dot(kNormal,kOrigin);

                //  ret = true;

                return ret;
            }

            #endregion
            #region bf_computeBestFitOBB

            //void bf_computeBestFitOBB(NxU32 vcount,const double *points,NxU32 pstride,double *sides,double *matrix,bool bruteForce)
            //{
            //  double plane[4];
            //  bf_computeBestFitPlane(vcount,points,pstride,0,0,plane);
            //  bf_planeToMatrix(plane,matrix);
            //  bf_computeOBB( vcount, points, pstride, sides, matrix );

            //  double refmatrix[16];
            //  memcpy(refmatrix,matrix,16*sizeof(double));

            //  double volume = sides[0]*sides[1]*sides[2];
            //  if ( bruteForce )
            //  {
            //    for (double a=10; a<180; a+=10)
            //    {
            //      double quat[4];
            //      bf_eulerToQuat(0,a*BF_DEG_TO_RAD,0,quat);
            //      double temp[16];
            //      double pmatrix[16];
            //      bf_quatToMatrix(quat,temp);
            //      bf_matrixMultiply(temp,refmatrix,pmatrix);
            //      double psides[3];
            //      bf_computeOBB( vcount, points, pstride, psides, pmatrix );
            //      double v = psides[0]*psides[1]*psides[2];
            //      if ( v < volume )
            //      {
            //        volume = v;
            //        memcpy(matrix,pmatrix,sizeof(double)*16);
            //        sides[0] = psides[0];
            //        sides[1] = psides[1];
            //        sides[2] = psides[2];
            //      }
            //    }
            //  }
            //}

            #endregion
            #region bf_computeBestFitOBB

            //void bf_computeBestFitOBB(NxU32 vcount,const double *points,NxU32 pstride,double *sides,double *pos,double *quat,bool bruteForce)
            //{
            //  double matrix[16];
            //  bf_computeBestFitOBB(vcount,points,pstride,sides,matrix,bruteForce);
            //  bf_getTranslation(matrix,pos);
            //  bf_matrixToQuat(matrix,quat);
            //}

            #endregion

            #region TEST_MAIN 1

            //#define TEST_MAIN 1

            //#if TEST_MAIN


            //static double points[] = {55.310600f, 217.810000f, 141.659000f,
            //179.067000f, 168.147000f, 228.722000f,  113.104000f, 73.912100f, 179.256000f,  71.613400f, 110.200000f, 137.975000f,  212.179000f, 151.760000f, 216.908000f,
            //47.166000f, 151.760000f, 96.693900f,  179.154000f, 72.552700f, 204.025000f,  162.637000f, 201.294000f, 171.002000f,  150.726000f, 209.554000f, 179.256000f,
            //146.129000f, 73.006900f, 195.769000f,  51.075400f, 143.504000f, 146.231000f,  195.667000f, 89.762000f, 228.794000f,  220.251000f, 109.784000f, 212.608000f,
            //129.617000f, 135.248000f, 151.976000f,  179.154000f, 160.016000f, 234.533000f,  80.079300f, 258.404000f, 186.495000f,  129.617000f, 75.683200f, 137.975000f,
            //162.642000f, 184.785000f, 148.828000f,  154.385000f, 105.632000f, 237.050000f,  214.520000f, 102.223000f, 195.769000f,  62.664900f, 184.785000f, 220.537000f,
            //162.642000f, 66.899100f, 187.512000f,  96.591800f, 209.554000f, 227.138000f,  47.054400f, 160.016000f, 193.835000f,  137.873000f, 66.567700f, 162.744000f,
            //203.923000f, 61.829800f, 154.488000f,  137.873000f, 135.248000f, 248.469000f,  187.410000f, 110.479000f, 241.251000f,  195.667000f, 135.248000f, 184.418000f,
            //88.335600f, 140.811000f, 245.306000f,  220.435000f, 110.479000f, 237.522000f,  228.692000f, 70.842000f, 204.025000f,  203.887000f, 110.501000f, 237.047000f,
            //136.389000f, 168.273000f, 129.719000f,  203.923000f, 77.454000f, 216.468000f,  222.831000f, 143.504000f, 228.794000f,  146.129000f, 151.760000f, 246.179000f,
            //104.848000f, 176.529000f, 135.517000f,  184.034000f, 93.966500f, 154.488000f,  137.873000f, 209.554000f, 167.375000f,  183.735000f, 160.016000f, 195.769000f,
            //104.848000f, 217.810000f, 223.446000f,  101.477000f, 135.248000f, 146.231000f,  90.820200f, 176.529000f, 129.719000f,  88.335600f, 110.479000f, 239.023000f,
            //104.848000f, 201.298000f, 230.467000f,  80.079300f, 97.167100f, 171.000000f,  228.692000f, 91.308600f, 212.281000f,  190.517000f, 102.223000f, 162.744000f,
            //96.591800f, 205.273000f, 228.794000f,  47.054400f, 217.810000f, 188.829000f,  96.591800f, 193.041000f, 232.342000f,  75.540400f, 176.529000f, 121.463000f,
            //228.692000f, 97.335100f, 237.050000f,  146.129000f, 85.710300f, 217.503000f,  50.727500f, 143.504000f, 137.975000f,  187.410000f, 118.735000f, 239.813000f,
            //187.410000f, 102.223000f, 160.954000f,  113.104000f, 135.248000f, 252.430000f,  48.405700f, 160.016000f, 96.693900f,  48.319900f, 151.760000f, 113.206000f,
            //121.361000f, 118.735000f, 142.193000f,  129.617000f, 110.479000f, 142.609000f,  54.412300f, 176.672000f, 104.775000f,  56.279700f, 143.504000f, 212.281000f,
            //166.582000f, 193.041000f, 220.537000f,  55.143400f, 160.016000f, 212.651000f,  170.898000f, 151.760000f, 163.266000f,  86.817200f, 118.735000f, 137.975000f,
            //41.493300f, 184.785000f, 187.512000f,  179.154000f, 126.991000f, 239.541000f,  140.231000f, 201.298000f, 146.231000f,  121.361000f, 184.785000f, 238.332000f,
            //72.771200f, 110.479000f, 220.537000f,  146.129000f, 203.866000f, 220.537000f,  96.591800f, 250.835000f, 156.360000f,  113.104000f, 143.504000f, 252.036000f,
            //141.567000f, 201.298000f, 162.744000f,  63.566900f, 184.785000f, 119.288000f,  104.848000f, 149.174000f, 146.231000f,  104.848000f, 168.273000f, 243.591000f,
            //168.991000f, 168.273000f, 162.744000f,  38.798200f, 176.529000f, 155.049000f,  187.410000f, 143.504000f, 177.567000f,  233.032000f, 102.223000f, 228.794000f,
            //74.211300f, 184.785000f, 228.794000f,  154.385000f, 193.041000f, 226.621000f,  212.179000f, 161.896000f, 228.794000f,  55.555600f, 135.752000f, 162.744000f,
            //46.966300f, 193.230000f, 129.519000f,  54.669400f, 135.248000f, 179.256000f,  220.435000f, 93.548100f, 195.769000f,  203.923000f, 151.760000f, 236.130000f,
            //96.591800f, 89.728000f, 129.719000f,  212.179000f, 102.223000f, 236.223000f,  63.566900f, 122.141000f, 121.463000f,  121.361000f, 135.248000f, 251.009000f,
            //146.129000f, 135.248000f, 157.772000f,  80.079300f, 258.835000f, 163.506000f,  165.685000f, 102.223000f, 237.050000f,  96.591800f, 151.760000f, 247.220000f,
            //55.236700f, 135.085000f, 196.674000f,  121.361000f, 171.376000f, 129.719000f,  182.036000f, 85.710300f, 146.231000f,  236.948000f, 83.697200f, 204.025000f,
            //146.129000f, 160.016000f, 243.576000f,  228.692000f, 66.096800f, 179.256000f,  75.299500f, 168.273000f, 113.206000f,  79.561700f, 250.946000f, 195.883000f,
            //145.903000f, 217.578000f, 203.620000f,  121.451000f, 250.903000f, 179.048000f,  79.180000f, 135.248000f, 238.088000f,  121.361000f, 176.529000f, 242.627000f,
            //121.598000f, 226.066000f, 212.367000f,  179.154000f, 59.706200f, 179.256000f,  96.591800f, 226.066000f, 160.504000f,  129.617000f, 226.066000f, 166.738000f,
            //80.079300f, 124.140000f, 237.050000f,  153.127000f, 118.357000f, 154.488000f,  63.566900f, 201.298000f, 133.569000f,  54.955300f, 235.070000f, 162.744000f,
            //179.154000f, 101.991000f, 237.311000f,  121.361000f, 246.016000f, 195.769000f,  179.154000f, 118.735000f, 240.240000f,  129.617000f, 151.760000f, 246.540000f,
            //88.335600f, 200.454000f, 146.838000f,  170.898000f, 196.421000f, 212.281000f,  90.573600f, 126.991000f, 137.975000f,  88.401500f, 201.267000f, 228.783000f,
            //96.591800f, 209.554000f, 155.231000f,  104.848000f, 193.041000f, 232.925000f,  216.578000f, 160.016000f, 228.794000f,  40.240800f, 168.273000f, 154.488000f,
            //181.479000f, 160.016000f, 212.281000f,  184.349000f, 143.504000f, 171.000000f,  43.019300f, 168.273000f, 179.256000f,  154.385000f, 68.518600f, 128.837000f,
            //83.204500f, 93.966500f, 212.281000f,  187.410000f, 126.991000f, 238.647000f,  220.496000f, 77.764800f, 219.867000f,  75.160000f, 126.991000f, 113.206000f,
            //137.873000f, 168.273000f, 242.403000f,  137.873000f, 201.298000f, 226.147000f,  113.104000f, 234.322000f, 208.191000f,  64.067700f, 119.452000f, 154.488000f,
            //76.261900f, 151.760000f, 237.050000f,  63.566900f, 168.273000f, 99.889000f,  179.154000f, 77.454000f, 134.003000f,  116.577000f, 110.479000f, 137.975000f,
            //71.823100f, 171.779000f, 113.206000f,  190.916000f, 160.016000f, 220.537000f,  179.154000f, 143.504000f, 165.582000f,  104.848000f, 135.248000f, 252.554000f,
            //104.848000f, 83.338200f, 212.281000f,  68.764100f, 168.273000f, 228.794000f,  220.435000f, 102.223000f, 208.392000f,  80.079300f, 252.338000f, 153.598000f,
            //146.129000f, 184.785000f, 131.625000f,  113.104000f, 74.272800f, 146.231000f,  187.410000f, 56.488000f, 154.488000f,  47.054400f, 221.357000f, 179.256000f,
            //170.898000f, 176.529000f, 230.561000f,  78.999600f, 102.223000f, 137.975000f,  48.648600f, 193.041000f, 204.025000f,  63.566900f, 209.477000f, 138.007000f,
            //129.617000f, 143.504000f, 247.686000f,  220.435000f, 85.710300f, 230.609000f,  44.614300f, 176.529000f, 195.769000f,  220.435000f, 128.098000f, 220.537000f,
            //88.335600f, 184.785000f, 134.975000f,  56.829100f, 234.322000f, 195.769000f,  113.104000f, 113.457000f, 137.975000f,  179.154000f, 151.760000f, 238.587000f,
            //228.573000f, 101.811000f, 220.717000f,  186.520000f, 168.273000f, 195.769000f,  42.084400f, 176.529000f, 137.975000f,  55.310600f, 193.041000f, 124.712000f,
            //88.335600f, 177.607000f, 129.101000f,  121.309000f, 159.997000f, 245.300000f,  170.898000f, 85.710300f, 222.266000f,  195.667000f, 143.504000f, 237.406000f,
            //47.099600f, 151.900000f, 162.744000f,  87.530500f, 135.248000f, 245.306000f,  137.873000f, 88.298700f, 220.537000f,  228.692000f, 67.070000f, 187.512000f,
            //96.591800f, 185.369000f, 137.975000f,  153.202000f, 151.760000f, 245.088000f,  71.823100f, 107.317000f, 154.488000f,  71.823100f, 176.529000f, 118.021000f,
            //121.361000f, 126.991000f, 250.945000f,  88.335600f, 259.848000f, 187.512000f,  162.642000f, 57.939400f, 154.488000f,  88.335600f, 184.785000f, 233.859000f,
            //212.179000f, 135.248000f, 240.508000f,  145.989000f, 69.165400f, 187.611000f,  195.961000f, 60.848500f, 146.018000f,  179.154000f, 57.436000f, 162.744000f,
            //170.898000f, 57.314400f, 154.488000f,  96.591800f, 102.223000f, 127.919000f,  179.154000f, 85.710300f, 143.820000f,  146.129000f, 77.454000f, 121.606000f,
            //154.385000f, 102.223000f, 143.596000f,  236.948000f, 76.857600f, 212.281000f,  174.229000f, 184.785000f, 171.000000f,  129.617000f, 94.953300f, 228.180000f,
            //71.823100f, 257.912000f, 171.000000f,  105.651000f, 201.088000f, 146.435000f,  55.310600f, 151.760000f, 90.261600f,  170.898000f, 85.710300f, 137.049000f,
            //92.876800f, 85.710300f, 154.488000f,  203.923000f, 89.409600f, 162.744000f,  54.458700f, 135.248000f, 129.719000f,  88.335600f, 98.616600f, 228.794000f,
            //146.129000f, 176.529000f, 130.899000f,  55.322000f, 152.091000f, 212.255000f,  195.667000f, 110.479000f, 239.093000f,  121.361000f, 158.723000f, 146.231000f,
            //51.415900f, 151.760000f, 204.025000f,  79.676500f, 193.715000f, 228.794000f,  96.591800f, 118.735000f, 245.933000f,  55.685100f, 136.729000f, 204.025000f,
            //171.011000f, 201.502000f, 178.935000f,  71.823100f, 143.504000f, 99.425000f,  121.361000f, 242.579000f, 198.880000f,  75.525600f, 102.223000f, 212.281000f,
            //104.546000f, 77.306700f, 154.488000f,  84.269500f, 126.991000f, 129.719000f,  96.591800f, 217.810000f, 223.077000f,  129.617000f, 226.066000f, 208.096000f,
            //154.385000f, 77.454000f, 206.123000f,  175.946000f, 193.041000f, 179.256000f,  137.873000f, 216.056000f, 212.281000f,  121.380000f, 94.059200f, 137.967000f,
            //195.667000f, 158.473000f, 228.794000f,  146.129000f, 110.479000f, 238.980000f,  179.282000f, 184.785000f, 178.943000f,  78.566200f, 102.223000f, 220.537000f,
            //129.617000f, 207.039000f, 154.488000f,  71.823100f, 254.988000f, 162.744000f,  88.335600f, 243.988000f, 204.025000f,  212.179000f, 151.133000f, 237.050000f,
            //162.642000f, 183.916000f, 146.231000f,  154.385000f, 110.479000f, 149.019000f,  137.873000f, 207.912000f, 220.537000f,  88.335600f, 88.261300f, 179.256000f,
            //52.810500f, 234.322000f, 187.512000f,  121.361000f, 126.837000f, 146.288000f,  122.345000f, 226.066000f, 162.315000f,  71.859800f, 127.181000f, 228.757000f,
            //80.079300f, 261.720000f, 179.256000f,  179.154000f, 164.456000f, 171.000000f,  121.361000f, 102.223000f, 239.256000f,  55.310600f, 231.470000f, 195.769000f,
            //74.316200f, 160.016000f, 104.950000f,  206.446000f, 126.991000f, 195.769000f,  113.104000f, 126.779000f, 146.436000f,  96.591800f, 117.436000f, 245.306000f,
            //137.873000f, 69.270800f, 178.703000f,  179.154000f, 56.723900f, 154.488000f,  96.591800f, 118.735000f, 143.393000f,  203.923000f, 110.479000f, 178.053000f,
            //96.591800f, 82.015800f, 171.000000f,  154.385000f, 184.785000f, 232.760000f,  195.667000f, 58.401600f, 154.488000f,  129.617000f, 143.504000f, 156.987000f,
            //63.566900f, 187.573000f, 220.537000f,  195.667000f, 139.425000f, 187.512000f,  179.154000f, 69.197800f, 130.998000f,  104.848000f, 250.835000f, 163.866000f,
            //150.903000f, 201.298000f, 220.537000f,  47.054400f, 158.040000f, 187.512000f,  129.617000f, 184.785000f, 237.802000f,  109.270000f, 193.041000f, 137.975000f,
            //45.859800f, 209.554000f, 146.231000f,  154.385000f, 126.991000f, 243.509000f,  187.410000f, 90.789100f, 228.794000f,  68.033200f, 118.735000f, 220.537000f,
            //187.410000f, 66.316700f, 195.769000f,  49.126900f, 226.066000f, 171.000000f,  55.310600f, 234.322000f, 159.875000f,  212.179000f, 79.199600f, 220.537000f,
            //49.980400f, 143.504000f, 113.206000f,  137.873000f, 126.991000f, 248.203000f,  104.848000f, 234.322000f, 210.348000f,  216.982000f, 110.479000f, 237.050000f,
            //80.079300f, 242.579000f, 148.772000f,  96.591800f, 84.231400f, 204.025000f,  93.502800f, 151.760000f, 129.719000f,  96.396300f, 102.157000f, 237.086000f,
            //88.335600f, 226.066000f, 215.538000f,  96.591800f, 88.900600f, 220.537000f,  45.313400f, 168.273000f, 121.463000f,  170.898000f, 206.742000f, 187.512000f,
            //55.310600f, 133.625000f, 187.512000f,  38.798200f, 193.041000f, 156.244000f,  96.591800f, 234.322000f, 210.972000f,  203.923000f, 80.983500f, 154.488000f,
            //121.361000f, 147.639000f, 162.744000f,  212.179000f, 74.886300f, 212.281000f,  162.642000f, 162.835000f, 237.050000f,  91.596500f, 85.710300f, 171.000000f,
            //187.410000f, 69.197800f, 200.446000f,  113.104000f, 102.223000f, 134.964000f,  36.704600f, 193.041000f, 162.744000f,  113.104000f, 234.106000f, 162.813000f,
            //97.462200f, 259.091000f, 162.744000f,  220.435000f, 97.862200f, 204.025000f,  104.848000f, 85.710300f, 132.243000f,  63.452000f, 209.554000f, 212.281000f,
            //80.949600f, 168.273000f, 237.050000f,  49.830400f, 217.810000f, 195.769000f,  71.823100f, 121.521000f, 121.463000f,  212.179000f, 61.826200f, 171.000000f,
            //137.873000f, 226.066000f, 201.434000f,  190.580000f, 118.735000f, 171.000000f,  148.840000f, 217.810000f, 195.769000f,  139.185000f, 201.298000f, 154.488000f,
            //162.629000f, 159.950000f, 162.749000f,  195.667000f, 93.966500f, 232.031000f,  220.435000f, 85.710300f, 175.156000f,  162.642000f, 118.735000f, 242.339000f,
            //212.179000f, 106.372000f, 195.769000f,  228.692000f, 68.956800f, 197.209000f,  207.909000f, 110.479000f, 187.512000f,  79.836600f, 192.936000f, 138.050000f,
            //170.898000f, 77.787600f, 212.281000f,  137.873000f, 160.016000f, 244.607000f,  117.485000f, 176.529000f, 129.719000f,  170.898000f, 110.479000f, 241.297000f,
            //191.688000f, 135.248000f, 179.256000f,  146.129000f, 102.223000f, 234.301000f,  162.642000f, 186.188000f, 228.794000f,  174.168000f, 126.991000f, 162.744000f,
            //129.570000f, 69.189400f, 154.406000f,  94.675400f, 118.735000f, 245.306000f,  95.239000f, 135.248000f, 139.021000f,  223.092000f, 77.454000f, 162.744000f,
            //71.823100f, 151.760000f, 99.707800f,  154.385000f, 193.208000f, 162.405000f,  129.617000f, 157.105000f, 245.306000f,  113.104000f, 81.845800f, 212.281000f,
            //220.435000f, 118.735000f, 217.365000f,  113.104000f, 155.208000f, 154.488000f,  137.282000f, 160.343000f, 146.231000f,  235.175000f, 85.710300f, 228.794000f,
            //228.692000f, 85.710300f, 189.974000f,  162.642000f, 59.402700f, 146.231000f,  203.923000f, 93.966500f, 233.172000f,  162.642000f, 118.735000f, 154.955000f,
            //76.200700f, 102.223000f, 171.000000f,  154.385000f, 118.735000f, 241.955000f,  170.898000f, 160.016000f, 166.333000f,  170.898000f, 184.785000f, 166.531000f,
            //116.121000f, 184.785000f, 129.719000f,  236.948000f, 82.042100f, 212.281000f,  88.335600f, 226.066000f, 157.209000f,  121.409000f, 201.288000f, 137.993000f,
            //170.898000f, 93.569900f, 228.794000f,  104.848000f, 114.341000f, 245.306000f,  88.863000f, 102.223000f, 129.996000f,  119.955000f, 151.760000f, 162.744000f,
            //113.104000f, 201.298000f, 143.291000f,  63.578100f, 250.826000f, 179.256000f,  47.054400f, 212.390000f, 146.231000f,  129.617000f, 238.677000f, 195.769000f,
            //129.617000f, 242.579000f, 182.243000f,  71.823100f, 176.529000f, 229.311000f,  50.981300f, 143.504000f, 179.256000f,  80.079300f, 110.479000f, 135.003000f,
            //162.642000f, 92.434500f, 137.975000f,  80.079300f, 118.735000f, 235.451000f,  212.179000f, 93.966500f, 174.432000f,  63.835800f, 119.396000f, 212.281000f,
            //47.064600f, 209.485000f, 195.737000f,  113.104000f, 163.256000f, 245.306000f,  137.873000f, 203.004000f, 146.231000f,  162.642000f, 209.554000f, 193.218000f,
            //67.805900f, 234.322000f, 146.231000f,  83.613400f, 93.966500f, 146.231000f,  129.617000f, 217.810000f, 165.060000f,  63.566900f, 180.525000f, 113.206000f,
            //74.612200f, 118.735000f, 228.794000f,  137.873000f, 69.197800f, 134.626000f,  228.692000f, 82.328600f, 179.256000f,  121.361000f, 81.106500f, 212.281000f,
            //235.810000f, 77.454000f, 179.256000f,  224.726000f, 110.479000f, 220.537000f,  113.104000f, 93.966500f, 230.239000f,  203.923000f, 143.504000f, 200.456000f,
            //80.079300f, 167.688000f, 236.910000f,  36.365000f, 184.785000f, 162.744000f,  71.823100f, 160.016000f, 102.923000f,  96.591800f, 82.292500f, 187.512000f,
            //202.140000f, 60.941500f, 154.488000f,  68.167200f, 168.273000f, 104.950000f,  96.591800f, 82.057400f, 179.256000f,  113.104000f, 169.095000f, 137.975000f,
            //168.625000f, 176.529000f, 154.488000f,  52.323700f, 209.554000f, 204.025000f,  154.385000f, 212.863000f, 195.769000f,  179.154000f, 57.476900f, 146.231000f,
            //104.848000f, 209.554000f, 227.221000f,  203.923000f, 151.760000f, 208.932000f,  134.861000f, 77.454000f, 129.719000f,  105.766000f, 166.428000f, 137.975000f,
            //74.096300f, 259.091000f, 171.000000f,  203.923000f, 77.454000f, 150.157000f,  131.409000f, 209.554000f, 162.744000f,  96.591800f, 217.810000f, 159.401000f,
            //162.642000f, 102.223000f, 145.102000f,  50.104400f, 226.066000f, 162.744000f,  80.079300f, 132.094000f, 113.206000f,  146.129000f, 201.298000f, 222.972000f,
            //187.410000f, 157.690000f, 212.281000f,  196.734000f, 110.479000f, 170.445000f,  179.377000f, 193.668000f, 186.608000f,  63.566900f, 226.066000f, 143.006000f,
            //236.865000f, 69.226000f, 187.512000f,  104.848000f, 92.224000f, 228.794000f,  71.823100f, 209.554000f, 143.181000f,  74.439600f, 250.835000f, 154.488000f,
            //220.435000f, 91.336500f, 187.512000f,  80.192700f, 242.550000f, 203.999000f,  45.717100f, 217.810000f, 179.256000f,  47.054400f, 201.298000f, 199.142000f,
            //146.129000f, 94.837100f, 227.611000f,  203.863000f, 118.597000f, 187.703000f,  100.395000f, 126.991000f, 146.231000f,  82.252600f, 135.248000f, 113.206000f,
            //134.392000f, 184.785000f, 237.050000f,  63.566900f, 168.273000f, 224.270000f,  170.898000f, 111.667000f, 154.488000f,  196.517000f, 94.647000f, 162.293000f,
            //204.358000f, 60.912100f, 179.487000f,  203.923000f, 70.696900f, 204.025000f,  203.923000f, 154.600000f, 212.281000f,  220.435000f, 93.966500f, 236.053000f,
            //133.726000f, 234.322000f, 195.769000f,  203.923000f, 85.710300f, 159.682000f,  80.079300f, 176.529000f, 233.803000f,  63.566900f, 135.248000f, 222.185000f,
            //45.669200f, 160.016000f, 137.975000f,  48.740400f, 151.760000f, 195.769000f,  159.489000f, 93.966500f, 228.794000f,  96.671000f, 143.122000f, 137.866000f,
            //137.873000f, 72.173800f, 129.719000f,  55.310600f, 224.369000f, 146.231000f,  42.238700f, 176.529000f, 187.512000f,  71.823100f, 116.261000f, 129.719000f,
            //58.885400f, 126.991000f, 121.463000f,  136.876000f, 68.626700f, 137.975000f,  96.591800f, 201.298000f, 148.766000f,  80.079300f, 98.331900f, 154.488000f,
            //96.591800f, 151.760000f, 134.103000f,  67.609100f, 143.504000f, 96.693900f,  162.642000f, 85.491700f, 220.757000f,  146.129000f, 66.577200f, 179.256000f,
            //179.154000f, 196.978000f, 195.769000f,  109.665000f, 259.091000f, 179.256000f,  236.948000f, 81.388500f, 187.512000f,  193.600000f, 85.710300f, 154.488000f,
            //129.617000f, 102.223000f, 140.607000f,  80.079300f, 176.529000f, 124.406000f,  44.258400f, 168.273000f, 187.512000f,  137.873000f, 231.816000f, 187.512000f,
            //111.354000f, 151.760000f, 154.488000f,  173.947000f, 143.504000f, 162.744000f,  212.179000f, 160.016000f, 232.234000f,  187.767000f, 160.016000f, 179.338000f,
            //47.047400f, 159.981000f, 121.463000f,  165.792000f, 151.760000f, 162.744000f,  64.746900f, 119.642000f, 129.719000f,  103.758000f, 143.504000f, 146.231000f,
            //71.823100f, 107.007000f, 212.281000f,  55.310600f, 134.077000f, 179.256000f,  104.848000f, 217.810000f, 159.072000f,  146.129000f, 154.876000f, 245.306000f,
            //127.401000f, 77.454000f, 137.975000f,  45.102300f, 176.529000f, 121.463000f,  129.617000f, 73.791900f, 195.769000f,  170.898000f, 135.248000f, 161.279000f,
            //184.481000f, 135.248000f, 171.000000f,  210.174000f, 110.479000f, 195.769000f,  137.873000f, 202.221000f, 154.488000f,  96.591800f, 232.570000f, 212.281000f,
            //137.873000f, 226.066000f, 177.340000f,  118.771000f, 77.454000f, 204.025000f,  122.909000f, 234.977000f, 204.025000f,  120.829000f, 160.016000f, 146.231000f,
            //96.591800f, 160.016000f, 244.585000f,  113.104000f, 118.735000f, 140.668000f,  96.450000f, 168.273000f, 129.836000f,  104.848000f, 76.831900f, 162.744000f,
            //58.936600f, 126.991000f, 179.256000f,  104.848000f, 135.248000f, 149.555000f,  129.486000f, 151.760000f, 162.744000f,  63.566900f, 250.835000f, 171.740000f,
            //203.923000f, 126.991000f, 191.950000f,  129.617000f, 160.016000f, 244.659000f,  96.591800f, 242.579000f, 205.035000f,  72.225100f, 234.511000f, 146.168000f,
            //146.129000f, 143.504000f, 247.466000f,  88.335600f, 217.810000f, 156.817000f,  71.823100f, 168.273000f, 109.890000f,  93.316300f, 85.710300f, 137.975000f,
            //88.335600f, 151.760000f, 243.071000f,  51.154800f, 143.504000f, 195.769000f,  55.310600f, 160.016000f, 91.718300f,  69.851000f, 110.479000f, 162.744000f,
            //88.335600f, 260.490000f, 162.744000f,  170.898000f, 83.842200f, 220.537000f,  96.591800f, 234.322000f, 159.856000f,  54.601000f, 176.529000f, 212.970000f,
            //137.873000f, 133.215000f, 154.488000f,  137.873000f, 143.504000f, 246.998000f,  228.692000f, 77.454000f, 221.744000f,  184.411000f, 102.223000f, 237.050000f,
            //80.079300f, 217.810000f, 152.071000f,  88.335600f, 118.735000f, 242.717000f,  104.603000f, 259.054000f, 187.430000f,  146.129000f, 93.966500f, 134.786000f,
            //203.923000f, 87.890000f, 228.794000f,  63.566900f, 248.031000f, 162.744000f,  63.731500f, 119.083000f, 146.231000f,  143.088000f, 209.554000f, 171.000000f,
            //64.065200f, 119.600000f, 162.744000f,  55.310600f, 143.504000f, 91.325700f,  113.104000f, 143.504000f, 157.783000f,  113.104000f, 193.041000f, 233.867000f,
            //137.873000f, 76.615300f, 204.025000f,  146.129000f, 85.710300f, 123.448000f,  182.136000f, 176.529000f, 179.256000f,  160.112000f, 85.710300f, 220.537000f,
            //55.310600f, 239.443000f, 179.256000f,  55.310600f, 189.900000f, 121.463000f,  170.898000f, 58.150300f, 146.231000f,  179.154000f, 110.479000f, 159.716000f,
            //154.385000f, 167.975000f, 145.674000f,  71.121800f, 176.529000f, 228.794000f,  63.566900f, 217.810000f, 140.688000f,  47.091000f, 184.656000f, 121.545000f,
            //154.385000f, 160.016000f, 240.887000f,  67.992300f, 176.529000f, 113.206000f,  149.909000f, 60.941500f, 154.488000f,  96.591800f, 110.479000f, 242.226000f,
            //114.321000f, 250.835000f, 171.000000f,  84.376900f, 93.966500f, 154.488000f,  82.233300f, 93.966500f, 204.025000f,  136.674000f, 224.972000f, 204.025000f,
            //104.848000f, 80.486500f, 137.975000f,  57.328200f, 201.298000f, 212.281000f,  129.617000f, 206.319000f, 146.231000f,  51.399900f, 143.504000f, 154.488000f,
            //162.642000f, 126.991000f, 157.437000f,  104.607000f, 242.528000f, 203.994000f,  37.871500f, 176.529000f, 162.744000f,  170.898000f, 95.070100f, 145.437000f,
            //63.566900f, 217.810000f, 208.752000f,  228.692000f, 102.223000f, 237.420000f,  220.334000f, 151.760000f, 228.794000f,  96.591800f, 105.463000f, 129.719000f,
            //217.847000f, 102.223000f, 204.025000f,  203.923000f, 135.248000f, 194.494000f,  54.010700f, 135.248000f, 121.463000f,  55.310600f, 226.066000f, 147.877000f,
            //121.360000f, 136.724000f, 153.348000f,  63.566900f, 130.424000f, 220.537000f,  170.898000f, 179.934000f, 162.744000f,  179.154000f, 118.092000f, 162.937000f,
            //212.179000f, 62.725600f, 162.744000f,  170.898000f, 143.504000f, 161.408000f,  113.104000f, 99.523500f, 237.050000f,  59.487500f, 126.991000f, 171.000000f,
            //134.925000f, 217.810000f, 212.281000f,  138.787000f, 193.041000f, 129.719000f,  63.566900f, 184.785000f, 221.270000f,  113.104000f, 168.273000f, 138.830000f,
            //82.632000f, 93.966500f, 187.512000f,  179.154000f, 135.248000f, 238.512000f,  146.129000f, 122.867000f, 154.488000f,  54.577200f, 135.248000f, 137.975000f,
            //84.605800f, 143.504000f, 113.206000f,  96.591800f, 112.259000f, 137.975000f,  182.599000f, 168.273000f, 212.281000f,  154.385000f, 69.197800f, 191.200000f,
            //88.335600f, 168.273000f, 239.688000f,  60.219600f, 176.529000f, 104.950000f,  179.154000f, 151.760000f, 165.329000f,  113.104000f, 93.966500f, 133.411000f,
            //96.591800f, 82.802900f, 195.769000f,  74.982100f, 102.223000f, 204.025000f,  40.581200f, 176.529000f, 146.231000f,  105.351000f, 77.652200f, 146.231000f,
            //146.129000f, 193.041000f, 140.866000f,  129.617000f, 157.505000f, 146.231000f,  63.557800f, 250.845000f, 170.934000f,  129.617000f, 167.670000f, 129.246000f,
            //105.397000f, 94.062900f, 129.543000f,  159.835000f, 60.941600f, 171.000000f,  146.129000f, 213.169000f, 179.256000f,  137.873000f, 163.375000f, 137.975000f,
            //192.348000f, 143.504000f, 187.512000f,  47.054400f, 176.529000f, 200.821000f,  121.361000f, 244.469000f, 171.000000f,  137.873000f, 71.621100f, 187.512000f,
            //54.336500f, 135.248000f, 187.512000f,  88.335600f, 210.804000f, 153.538000f,  209.553000f, 102.223000f, 179.256000f,  96.589600f, 93.965800f, 228.795000f,
            //113.104000f, 229.588000f, 212.281000f,  146.129000f, 88.217500f, 220.537000f,  64.806900f, 193.724000f, 129.232000f,  80.079300f, 234.322000f, 209.635000f,
            //118.617000f, 102.223000f, 137.975000f,  71.823100f, 135.248000f, 231.084000f,  187.410000f, 61.498400f, 138.950000f,  170.898000f, 184.785000f, 226.190000f,
            //66.391100f, 250.835000f, 187.512000f,  63.566900f, 201.298000f, 216.488000f,  96.591800f, 243.787000f, 204.025000f,  203.923000f, 75.606900f, 146.231000f,
            //187.410000f, 106.112000f, 162.744000f,  187.410000f, 149.324000f, 187.512000f,  113.104000f, 135.868000f, 153.969000f,  207.665000f, 151.760000f, 212.281000f,
            //113.104000f, 256.261000f, 187.512000f,  146.129000f, 64.856300f, 137.975000f,  171.871000f, 201.298000f, 204.025000f,  121.361000f, 187.019000f, 237.050000f,
            //38.826600f, 184.785000f, 154.488000f,  47.054400f, 151.760000f, 98.843000f,  67.450200f, 143.504000f, 228.794000f,  222.858000f, 85.710300f, 179.256000f,
            //129.617000f, 193.041000f, 126.004000f,  59.320200f, 126.991000f, 195.769000f,  88.335600f, 107.315000f, 237.050000f,  129.617000f, 168.273000f, 243.236000f,
            //63.260600f, 242.579000f, 195.769000f,  195.667000f, 77.425500f, 146.301000f,  63.566900f, 164.025000f, 96.693900f,  195.667000f, 126.991000f, 180.931000f,
            //63.823400f, 119.068000f, 195.769000f,  159.356000f, 93.966500f, 137.975000f,  44.142600f, 160.016000f, 146.231000f,  71.823100f, 238.843000f, 204.025000f,
            //104.848000f, 188.743000f, 137.975000f,  162.642000f, 93.966500f, 139.615000f,  46.905700f, 201.671000f, 137.975000f,  217.808000f, 85.710300f, 171.000000f,
            //129.617000f, 217.810000f, 215.406000f,  60.912700f, 176.529000f, 220.537000f,  167.798000f, 184.785000f, 162.744000f,  162.714000f, 201.325000f, 212.328000f,
            //80.079300f, 121.196000f, 129.719000f,  122.544000f, 242.579000f, 170.407000f,  141.892000f, 226.066000f, 187.512000f,  129.617000f, 209.554000f, 222.272000f,
            //44.577000f, 160.016000f, 171.000000f,  203.923000f, 135.248000f, 240.010000f,  104.848000f, 168.273000f, 136.779000f,  170.898000f, 150.118000f, 162.744000f,
            //45.005300f, 168.273000f, 129.719000f,  153.052000f, 184.785000f, 138.944000f,  194.305000f, 126.991000f, 179.256000f,  137.873000f, 74.154000f, 195.769000f,
            //129.617000f, 203.046000f, 137.975000f,  88.335600f, 251.665000f, 196.350000f,  63.566900f, 231.038000f, 204.025000f,  71.823100f, 209.554000f, 216.558000f,
            //162.642000f, 75.256100f, 204.025000f,  146.129000f, 220.762000f, 195.769000f,  236.948000f, 77.454000f, 180.677000f,  137.873000f, 151.760000f, 245.985000f,
            //71.823100f, 126.991000f, 108.911000f,  212.179000f, 77.454000f, 217.463000f,  96.591800f, 82.335300f, 162.744000f,  88.335600f, 110.479000f, 136.236000f,
            //187.410000f, 171.781000f, 187.512000f,  55.310600f, 209.554000f, 137.210000f,  104.848000f, 93.966500f, 231.081000f,  104.848000f, 193.041000f, 140.367000f,
            //195.667000f, 75.443600f, 212.281000f,  121.361000f, 71.213200f, 179.256000f,  41.815800f, 193.041000f, 187.512000f,  88.087900f, 217.946000f, 220.609000f,
            //104.848000f, 85.710300f, 218.295000f,  137.873000f, 67.998100f, 171.000000f,  187.410000f, 93.966500f, 156.787000f,  88.335600f, 93.153500f, 220.537000f,
            //137.873000f, 217.810000f, 171.710000f,  71.823100f, 184.785000f, 227.402000f,  43.544700f, 209.554000f, 154.488000f,  96.591800f, 143.504000f, 249.879000f,
            //236.948000f, 71.988100f, 204.025000f,  55.322400f, 135.275000f, 146.096000f,  203.923000f, 147.269000f, 204.025000f,  80.079300f, 97.030600f, 212.281000f,
            //154.385000f, 85.710300f, 123.858000f,  104.848000f, 143.504000f, 252.058000f,  240.702000f, 77.454000f, 187.512000f,  96.591800f, 242.579000f, 157.446000f,
            //63.566900f, 143.504000f, 224.449000f,  220.435000f, 64.239600f, 179.256000f,  121.361000f, 110.479000f, 246.061000f,  154.385000f, 201.298000f, 218.356000f,
            //49.082500f, 151.760000f, 121.463000f,  129.617000f, 155.963000f, 154.488000f,  187.410000f, 81.794100f, 146.231000f,  88.335600f, 254.707000f, 154.488000f,
            //187.410000f, 135.248000f, 174.232000f,  232.872000f, 69.197800f, 171.000000f,  183.613000f, 60.941600f, 137.975000f,  41.222200f, 201.298000f, 154.488000f,
            //46.392500f, 160.016000f, 113.206000f,  80.079300f, 96.747700f, 195.769000f,  144.694000f, 193.041000f, 137.975000f,  133.842000f, 226.066000f, 171.000000f,
            //137.080000f, 119.166000f, 245.110000f,  55.310600f, 143.504000f, 210.012000f,  154.936000f, 193.041000f, 162.744000f,  80.079300f, 172.453000f, 121.463000f,
            //88.335600f, 90.106400f, 154.488000f,  109.450000f, 77.454000f, 195.769000f,  185.932000f, 176.529000f, 187.512000f,  138.488000f, 69.197800f, 179.256000f,
            //88.335600f, 234.322000f, 210.632000f,  63.566900f, 248.443000f, 187.512000f,  179.154000f, 102.223000f, 156.086000f,  187.410000f, 124.274000f, 171.000000f,
            //55.310600f, 215.338000f, 204.025000f,  68.790200f, 110.479000f, 146.231000f,  87.225500f, 143.504000f, 121.463000f,  105.839000f, 77.716000f, 187.512000f,
            //203.923000f, 143.504000f, 238.812000f,  87.145700f, 93.966500f, 220.537000f,  121.361000f, 77.454000f, 140.387000f,  51.299000f, 143.504000f, 171.000000f,
            //148.848000f, 193.041000f, 146.231000f,  195.667000f, 151.760000f, 234.317000f,  80.079300f, 226.066000f, 213.488000f,  103.799000f, 77.454000f, 162.744000f,
            //104.848000f, 126.991000f, 250.732000f,  228.692000f, 93.966500f, 236.401000f,  96.591800f, 260.312000f, 187.512000f,  92.485600f, 242.579000f, 154.488000f,
            //42.249300f, 209.554000f, 162.744000f,  212.179000f, 113.471000f, 237.050000f,  126.420000f, 168.273000f, 129.719000f,  121.361000f, 168.273000f, 243.915000f,
            //195.667000f, 102.711000f, 236.839000f,  129.617000f, 90.036600f, 137.975000f,  199.452000f, 143.504000f, 195.769000f,  179.154000f, 135.248000f, 166.231000f,
            //113.104000f, 209.554000f, 151.835000f,  88.335600f, 242.579000f, 151.934000f,  172.977000f, 93.966500f, 146.231000f,  170.898000f, 64.928000f, 187.512000f,
            //228.692000f, 77.680800f, 170.286000f,  64.658900f, 193.041000f, 219.809000f,  47.055700f, 176.524000f, 113.215000f,  84.194200f, 168.273000f, 121.463000f,
            //50.708600f, 160.016000f, 204.025000f,  179.154000f, 67.333100f, 195.769000f,  121.361000f, 72.613600f, 187.512000f,  104.848000f, 183.631000f, 236.576000f,
            //210.773000f, 135.248000f, 204.025000f,  146.562000f, 177.237000f, 237.156000f,  63.566900f, 151.760000f, 93.789900f,  207.281000f, 118.735000f, 195.769000f,
            //242.910000f, 77.454000f, 195.769000f,  93.009500f, 209.554000f, 154.488000f,  142.689000f, 69.197800f, 129.719000f,  146.129000f, 210.285000f, 213.043000f,
            //104.848000f, 205.461000f, 228.794000f,  80.079300f, 184.785000f, 231.906000f,  220.435000f, 87.906600f, 179.256000f,  81.598200f, 259.091000f, 162.744000f,
            //113.104000f, 74.999000f, 187.512000f,  113.104000f, 168.273000f, 244.423000f,  121.361000f, 209.554000f, 225.075000f,  47.054400f, 220.434000f, 162.744000f,
            //170.898000f, 193.041000f, 217.011000f,  162.735000f, 209.571000f, 187.292000f,  71.544900f, 201.653000f, 220.661000f,  113.104000f, 242.579000f, 202.006000f,
            //170.898000f, 62.060900f, 179.256000f,  220.435000f, 70.939100f, 204.025000f,  47.000500f, 151.760000f, 105.978000f,  45.225300f, 217.810000f, 171.000000f,
            //186.111000f, 151.760000f, 171.000000f,  88.347200f, 135.217000f, 129.676000f,  129.617000f, 230.498000f, 204.025000f,  179.154000f, 62.650600f, 187.512000f,
            //146.149000f, 168.263000f, 137.961000f,  121.361000f, 70.393500f, 171.000000f,  83.726500f, 217.810000f, 154.488000f,  104.848000f, 110.479000f, 243.519000f,
            //55.140000f, 135.094000f, 96.557800f,  113.104000f, 92.936100f, 228.794000f,  71.823100f, 249.478000f, 154.488000f,  215.781000f, 135.248000f, 212.281000f,
            //41.720100f, 209.554000f, 171.000000f,  162.642000f, 93.966500f, 228.875000f,  162.642000f, 193.041000f, 167.062000f,  71.823100f, 107.109000f, 195.769000f,
            //71.823100f, 118.735000f, 125.865000f,  48.739300f, 151.760000f, 179.256000f,  220.435000f, 84.083700f, 228.794000f,  220.435000f, 67.525100f, 195.769000f,
            //80.079300f, 217.810000f, 216.915000f,  129.617000f, 221.337000f, 212.281000f,  88.335600f, 174.142000f, 237.050000f,  59.916400f, 126.991000f, 146.231000f,
            //63.566900f, 173.371000f, 104.950000f,  129.216000f, 242.107000f, 179.256000f,  119.069000f, 93.966500f, 228.794000f,  179.154000f, 93.966500f, 150.865000f,
            //113.104000f, 77.984800f, 138.673000f,  79.537900f, 151.760000f, 105.558000f,  97.908000f, 85.710300f, 212.281000f,  121.361000f, 94.167500f, 228.494000f,
            //71.823100f, 168.273000f, 231.425000f,  69.630000f, 110.479000f, 179.256000f,  60.405300f, 126.991000f, 154.488000f,  137.873000f, 193.496000f, 129.389000f,
            //121.361000f, 110.479000f, 140.209000f,  154.385000f, 67.465100f, 187.512000f,  203.923000f, 75.071100f, 212.281000f,  55.204400f, 194.140000f, 212.445000f,
            //185.392000f, 176.529000f, 204.025000f,  113.104000f, 204.627000f, 228.794000f,  71.823100f, 106.881000f, 146.231000f,  144.963000f, 176.529000f, 129.719000f,
            //224.957000f, 110.479000f, 237.050000f,  96.591800f, 198.112000f, 146.231000f,  83.411600f, 93.966500f, 137.975000f,  162.642000f, 176.529000f, 233.132000f,
            //71.823100f, 226.066000f, 210.411000f,  48.309900f, 143.504000f, 104.950000f,  129.617000f, 211.828000f, 162.744000f,  187.369000f, 152.416000f, 179.256000f,
            //113.104000f, 193.041000f, 135.595000f,  212.179000f, 69.197800f, 200.853000f,  74.142200f, 259.091000f, 179.256000f,  154.385000f, 206.092000f, 212.281000f,
            //38.798200f, 184.785000f, 175.522000f,  71.365000f, 200.590000f, 138.365000f,  154.385000f, 80.906700f, 212.281000f,  179.154000f, 110.479000f, 241.651000f,
            //220.435000f, 143.504000f, 224.578000f,  232.651000f, 69.197800f, 195.769000f,  85.761900f, 110.479000f, 237.050000f,  175.694000f, 201.298000f, 187.512000f,
            //60.603400f, 126.991000f, 212.281000f,  47.524900f, 152.899000f, 171.000000f,  104.848000f, 231.882000f, 212.281000f,  137.873000f, 157.863000f, 154.488000f,
            //55.310600f, 209.554000f, 206.913000f,  211.950000f, 126.991000f, 204.298000f,  204.031000f, 60.920500f, 162.231000f,  187.410000f, 85.710300f, 224.867000f,
            //195.667000f, 60.941500f, 182.910000f,  113.104000f, 184.785000f, 237.537000f,  170.898000f, 57.864900f, 162.744000f,  179.154000f, 160.016000f, 168.564000f,
            //104.848000f, 99.886600f, 237.050000f,  76.297500f, 102.223000f, 154.488000f,  195.667000f, 59.589600f, 171.000000f,  80.079300f, 201.298000f, 144.075000f,
            //92.513900f, 85.710300f, 146.231000f,  146.387000f, 218.272000f, 186.470000f,  113.104000f, 226.066000f, 159.812000f,  154.385000f, 105.980000f, 146.231000f,
            //104.848000f, 226.066000f, 160.539000f,  38.993100f, 200.434000f, 171.000000f,  55.310600f, 238.161000f, 171.000000f,  71.823100f, 193.041000f, 133.640000f,
            //154.385000f, 118.735000f, 154.870000f,  63.566900f, 124.158000f, 104.950000f,  104.848000f, 91.695100f, 129.719000f,  58.045000f, 135.248000f, 212.281000f,
            //162.642000f, 60.941500f, 141.294000f,  129.951000f, 102.175000f, 237.119000f,  80.079300f, 205.220000f, 146.231000f,  154.385000f, 71.718000f, 195.769000f,
            //170.898000f, 193.041000f, 173.367000f,  162.642000f, 151.760000f, 242.061000f,  79.264300f, 108.881000f, 228.794000f,  187.410000f, 102.478000f, 236.920000f,
            //187.410000f, 149.084000f, 179.256000f,  55.026600f, 168.347000f, 96.630600f,  154.385000f, 61.063200f, 146.864000f,  80.079300f, 234.322000f, 149.324000f,
            //129.617000f, 87.400600f, 220.537000f,  146.129000f, 62.617600f, 162.744000f,  104.848000f, 176.529000f, 240.483000f,  121.361000f, 160.016000f, 143.621000f,
            //146.129000f, 160.016000f, 154.811000f,  162.642000f, 93.890400f, 228.794000f,  214.041000f, 110.479000f, 204.025000f,  187.410000f, 168.273000f, 194.154000f,
            //104.625000f, 209.692000f, 154.398000f,  80.079300f, 100.476000f, 137.975000f,  146.129000f, 195.319000f, 146.231000f,  212.172000f, 118.659000f, 204.040000f,
            //212.179000f, 151.760000f, 236.786000f,  64.949000f, 234.322000f, 203.418000f,  113.104000f, 151.760000f, 249.416000f,  67.800000f, 151.760000f, 96.693900f,
            //228.692000f, 66.810800f, 171.000000f,  241.213000f, 77.454000f, 204.025000f,  137.873000f, 85.710300f, 217.676000f,  234.195000f, 85.710300f, 220.537000f,
            //104.848000f, 102.223000f, 238.918000f,  48.816600f, 151.760000f, 129.719000f,  179.154000f, 184.785000f, 217.147000f,  154.414000f, 135.195000f, 245.314000f,
            //88.335600f, 143.504000f, 124.453000f,  66.481100f, 250.835000f, 162.744000f,  154.385000f, 199.399000f, 220.537000f,  96.591800f, 259.091000f, 189.121000f,
            //216.701000f, 126.991000f, 212.281000f,  162.642000f, 110.479000f, 240.905000f,  183.520000f, 168.273000f, 204.025000f,  55.310600f, 198.992000f, 129.719000f,
            //179.154000f, 176.529000f, 224.529000f,  57.791700f, 242.579000f, 171.000000f,  48.667000f, 226.066000f, 179.256000f,  203.923000f, 66.001600f, 195.769000f,
            //88.335600f, 102.223000f, 232.971000f,  50.560800f, 143.504000f, 187.512000f,  195.667000f, 85.710300f, 155.678000f,  165.028000f, 184.785000f, 154.488000f,
            //162.642000f, 110.479000f, 149.496000f,  146.129000f, 194.167000f, 228.794000f,  47.054400f, 151.760000f, 154.487000f,  71.823100f, 234.322000f, 206.945000f,
            //52.026300f, 226.066000f, 154.488000f,  69.577000f, 110.479000f, 187.512000f,  224.590000f, 126.991000f, 228.794000f,  195.667000f, 85.710300f, 225.321000f,
            //162.642000f, 135.248000f, 158.201000f,  154.385000f, 93.966500f, 135.502000f,  65.649000f, 184.785000f, 121.463000f,  104.848000f, 110.479000f, 135.100000f,
            //170.898000f, 118.735000f, 157.914000f,  47.168000f, 160.846000f, 195.769000f,  170.723000f, 60.926300f, 137.931000f,  165.209000f, 184.785000f, 228.794000f,
            //212.179000f, 62.021800f, 179.256000f,  229.412000f, 77.454000f, 171.000000f,  43.738400f, 168.273000f, 137.975000f,  204.358000f, 136.577000f, 195.769000f,
            //146.129000f, 126.991000f, 155.951000f,  232.009000f, 85.710300f, 204.025000f,  203.833000f, 102.028000f, 171.108000f,  162.642000f, 143.504000f, 243.112000f,
            //92.275700f, 85.710300f, 162.744000f,  154.385000f, 210.885000f, 204.025000f,  148.735000f, 193.041000f, 228.794000f,  88.335600f, 250.835000f, 152.259000f,
            //170.898000f, 180.517000f, 228.794000f,  172.125000f, 85.710300f, 137.975000f,  129.617000f, 70.708300f, 146.231000f,  121.361000f, 250.835000f, 188.809000f,
            //88.335600f, 263.569000f, 179.256000f,  104.628000f, 242.579000f, 162.915000f,  43.657100f, 184.785000f, 129.719000f,  220.435000f, 77.454000f, 159.808000f,
            //121.361000f, 109.429000f, 245.306000f,  210.847000f, 85.574400f, 228.794000f,  46.313200f, 160.016000f, 104.950000f,  113.104000f, 217.810000f, 157.739000f,
            //88.335600f, 188.391000f, 137.975000f,  92.574200f, 151.760000f, 245.306000f,  154.385000f, 160.016000f, 158.722000f,  129.617000f, 211.833000f, 220.537000f,
            //172.681000f, 77.454000f, 212.281000f,  103.740000f, 250.835000f, 162.744000f,  137.306000f, 200.960000f, 139.016000f,  217.674000f, 143.504000f, 220.537000f,
            //84.744700f, 151.760000f, 113.206000f,  104.848000f, 125.901000f, 146.885000f,  121.361000f, 73.021100f, 146.231000f,  55.310600f, 135.248000f, 171.486000f,
            //137.873000f, 135.248000f, 155.265000f,  88.335600f, 89.398800f, 146.231000f,  195.667000f, 60.116500f, 179.256000f,  146.129000f, 91.318900f, 129.719000f,
            //71.823100f, 193.041000f, 224.532000f,  88.335600f, 88.789300f, 195.769000f,  60.165700f, 168.273000f, 220.537000f,  161.107000f, 184.785000f, 146.231000f,
            //162.642000f, 77.454000f, 125.941000f,  71.823100f, 106.225000f, 204.025000f,  80.079300f, 98.009000f, 146.231000f,  47.556400f, 184.785000f, 202.715000f,
            //56.791900f, 217.810000f, 204.025000f,  195.667000f, 77.454000f, 215.484000f,  54.991600f, 168.273000f, 212.800000f,  113.104000f, 204.405000f, 146.231000f,
            //187.410000f, 154.608000f, 204.025000f,  187.410000f, 93.966500f, 231.037000f,  49.326100f, 226.066000f, 187.512000f,  78.597500f, 118.735000f, 129.719000f,
            //104.453000f, 102.223000f, 129.961000f,  146.129000f, 62.248200f, 154.488000f,  71.823100f, 254.493000f, 187.512000f,  121.363000f, 193.040000f, 129.720000f,
            //113.104000f, 196.115000f, 137.975000f,  212.179000f, 71.003000f, 204.025000f,  187.410000f, 85.710300f, 150.420000f,  203.923000f, 102.223000f, 235.913000f,
            //154.385000f, 59.531800f, 162.744000f,  173.550000f, 135.248000f, 162.744000f,  228.692000f, 88.177000f, 204.025000f,  63.566900f, 121.785000f, 113.206000f,
            //104.848000f, 77.454000f, 150.081000f,  104.848000f, 118.735000f, 141.896000f,  80.079300f, 102.223000f, 223.222000f,  91.326400f, 85.710300f, 179.256000f,
            //80.079300f, 184.785000f, 131.176000f,  154.385000f, 77.454000f, 121.889000f,  48.535000f, 143.504000f, 96.693900f,  76.008100f, 102.223000f, 162.744000f,
            //55.330700f, 135.297000f, 171.000000f,  88.335600f, 234.322000f, 154.788000f,  121.361000f, 143.504000f, 159.379000f,  71.823100f, 242.579000f, 201.213000f,
            //113.104000f, 77.454000f, 200.093000f,  62.252500f, 135.248000f, 220.537000f,  113.104000f, 176.529000f, 242.402000f,  43.083700f, 193.041000f, 137.975000f,
            //204.684000f, 110.479000f, 179.256000f,  71.823100f, 184.785000f, 126.672000f,  55.310600f, 133.646000f, 129.719000f,  112.637000f, 250.750000f, 195.669000f,
            //137.634000f, 217.087000f, 171.000000f,  80.079300f, 226.066000f, 151.765000f,  80.079300f, 160.016000f, 110.780000f,  46.924600f, 218.339000f, 187.512000f,
            //88.335600f, 168.273000f, 124.445000f,  55.310600f, 217.810000f, 202.634000f,  121.361000f, 234.322000f, 164.582000f,  187.410000f, 160.016000f, 175.829000f,
            //80.079300f, 228.777000f, 212.281000f,  137.873000f, 203.838000f, 162.744000f,  212.179000f, 90.875900f, 171.000000f,  170.604000f, 69.197800f, 129.737000f,
            //154.385000f, 143.504000f, 246.287000f,  183.328000f, 168.273000f, 220.537000f,  137.873000f, 184.785000f, 123.283000f,  102.872000f, 160.016000f, 137.975000f,
            //71.823100f, 160.016000f, 233.820000f,  220.103000f, 135.248000f, 221.285000f,  71.823100f, 248.515000f, 195.769000f,  64.719700f, 151.760000f, 227.647000f,
            //40.734400f, 184.785000f, 146.231000f,  42.904200f, 201.298000f, 187.512000f,  175.859000f, 176.529000f, 171.000000f,  121.361000f, 86.168700f, 219.904000f,
            //170.199000f, 159.792000f, 236.962000f,  76.274300f, 226.066000f, 212.281000f,  179.154000f, 60.941500f, 183.081000f,  229.880000f, 102.223000f, 237.050000f,
            //137.873000f, 113.337000f, 146.231000f,  55.310600f, 234.322000f, 193.630000f,  170.898000f, 206.098000f, 195.769000f,  154.385000f, 211.123000f, 187.512000f,
            //146.129000f, 193.041000f, 229.735000f,  137.873000f, 151.760000f, 160.203000f,  113.104000f, 126.991000f, 251.033000f,  69.385200f, 110.479000f, 195.769000f,
            //39.932500f, 193.041000f, 179.256000f,  137.873000f, 184.005000f, 236.754000f,  53.225700f, 143.504000f, 204.025000f,  228.692000f, 106.871000f, 237.050000f,
            //47.487200f, 153.265000f, 137.975000f,  129.617000f, 76.135900f, 204.025000f,  121.361000f, 157.099000f, 154.488000f,  146.129000f, 102.223000f, 143.108000f,
            //71.823100f, 110.479000f, 218.639000f,  220.435000f, 83.638000f, 171.000000f,  170.898000f, 201.622000f, 204.650000f,  59.282200f, 126.991000f, 129.719000f,
            //220.435000f, 66.289400f, 162.744000f,  179.154000f, 168.273000f, 172.835000f,  63.445700f, 118.735000f, 204.025000f,  220.435000f, 151.447000f, 228.794000f,
            //41.704500f, 184.785000f, 137.975000f,  63.566900f, 186.323000f, 121.463000f,  96.591800f, 126.991000f, 143.590000f,  154.385000f, 102.223000f, 235.093000f,
            //91.697200f, 85.710300f, 187.512000f,  137.788000f, 93.996900f, 137.949000f,  69.259700f, 110.479000f, 212.281000f,  170.898000f, 174.564000f, 162.744000f,
            //146.129000f, 63.262400f, 146.231000f,  37.602900f, 193.041000f, 171.000000f,  113.104000f, 184.785000f, 131.886000f,  104.848000f, 222.054000f, 220.537000f,
            //203.923000f, 85.710300f, 226.987000f,  129.617000f, 93.966500f, 139.492000f,  121.659000f, 251.033000f, 187.512000f,  137.873000f, 209.554000f, 218.915000f,
            //195.667000f, 66.065800f, 195.769000f,  162.642000f, 84.977500f, 129.719000f,  146.129000f, 184.785000f, 234.918000f,  75.592000f, 102.223000f, 195.769000f,
            //75.994600f, 102.223000f, 187.512000f,  71.823100f, 107.755000f, 162.744000f,  186.664000f, 176.529000f, 195.769000f,  179.154000f, 176.529000f, 175.116000f,
            //121.361000f, 176.529000f, 126.716000f,  137.873000f, 77.454000f, 206.135000f,  195.667000f, 118.735000f, 175.363000f,  220.435000f, 65.095500f, 187.512000f,
            //51.079400f, 143.504000f, 121.463000f,  212.179000f, 126.991000f, 239.567000f,  237.063000f, 77.454000f, 212.674000f,  63.566900f, 242.579000f, 154.802000f,
            //71.035800f, 217.810000f, 212.602000f,  88.920300f, 143.504000f, 244.952000f,  154.385000f, 204.258000f, 171.000000f,  104.778000f, 77.423900f, 179.844000f,
            //47.054400f, 157.097000f, 113.206000f,  113.104000f, 185.602000f, 237.050000f,  52.320500f, 234.322000f, 179.256000f,  163.075000f, 167.550000f, 153.802000f,
            //154.385000f, 209.554000f, 206.788000f,  236.986000f, 69.157400f, 179.626000f,  55.310600f, 201.298000f, 210.590000f,  47.219400f, 152.244000f, 146.231000f,
            //179.154000f, 82.408200f, 220.537000f,  83.331600f, 93.966500f, 162.744000f,  84.733500f, 259.091000f, 187.512000f,  162.642000f, 85.710300f, 130.931000f,
            //187.410000f, 160.016000f, 229.298000f,  50.121400f, 168.273000f, 204.025000f,  137.873000f, 169.454000f, 129.719000f,  212.179000f, 118.735000f, 238.128000f,
            //113.104000f, 85.710300f, 133.348000f,  196.416000f, 151.972000f, 203.596000f,  55.310600f, 226.066000f, 198.585000f,  228.692000f, 110.479000f, 236.108000f,
            //143.280000f, 217.810000f, 179.256000f,  137.873000f, 155.933000f, 245.306000f,  129.617000f, 176.529000f, 241.730000f,  96.591800f, 110.479000f, 135.814000f,
            //129.677000f, 234.472000f, 170.900000f,  121.361000f, 70.266600f, 162.744000f,  113.104000f, 160.016000f, 149.105000f,  129.617000f, 135.248000f, 249.340000f,
            //88.335600f, 89.274500f, 162.744000f,  212.179000f, 85.710300f, 164.687000f,  212.179000f, 146.343000f, 212.281000f,  177.307000f, 184.785000f, 220.537000f,
            //181.769000f, 176.529000f, 220.537000f,  104.848000f, 151.760000f, 249.211000f,  82.052300f, 160.016000f, 113.206000f,  63.784100f, 119.070000f, 187.512000f,
            //80.079300f, 110.479000f, 231.148000f,  161.663000f, 209.554000f, 195.769000f,  88.335600f, 193.041000f, 231.771000f,  79.861900f, 143.504000f, 105.355000f,
            //82.562600f, 93.966500f, 195.769000f,  121.361000f, 76.965900f, 204.025000f,  80.079300f, 97.059000f, 187.512000f,  203.923000f, 111.681000f, 179.256000f,
            //137.873000f, 160.016000f, 149.003000f,  88.335600f, 90.392600f, 212.281000f,  221.335000f, 126.991000f, 237.456000f,  55.310600f, 183.738000f, 113.206000f,
            //47.040300f, 217.841000f, 154.408000f,  129.617000f, 77.454000f, 207.050000f,  88.335600f, 151.760000f, 121.463000f,  46.989800f, 159.738000f, 129.719000f,
            //96.591800f, 208.587000f, 154.488000f,  121.361000f, 226.280000f, 212.281000f,  228.692000f, 76.790800f, 220.537000f,  162.642000f, 60.941500f, 171.789000f,
            //187.410000f, 164.525000f, 179.256000f,  162.642000f, 189.157000f, 162.744000f,  110.428000f, 143.504000f, 154.488000f,  154.385000f, 86.360000f, 219.873000f,
            //59.199500f, 126.991000f, 137.975000f,  181.213000f, 184.785000f, 212.281000f,  88.335600f, 176.529000f, 236.032000f,  220.435000f, 145.018000f, 237.050000f,
            //232.477000f, 77.454000f, 220.537000f,  162.642000f, 135.248000f, 242.125000f,  50.660600f, 135.248000f, 104.950000f,  57.359600f, 234.322000f, 154.488000f,
            //79.956800f, 127.167000f, 121.834000f,  228.692000f, 112.484000f, 228.794000f,  80.079300f, 168.273000f, 117.767000f,  171.380000f, 110.479000f, 154.179000f,
            //203.527000f, 160.765000f, 228.794000f,  80.079300f, 102.223000f, 136.742000f,  61.112700f, 168.273000f, 96.693900f,  93.788900f, 250.835000f, 154.488000f,
            //134.342000f, 234.322000f, 179.256000f,  162.642000f, 60.650800f, 171.000000f,  129.643000f, 242.611000f, 187.839000f,  113.104000f, 176.529000f, 132.926000f,
            //82.677700f, 93.966500f, 179.256000f,  179.154000f, 155.462000f, 237.050000f,  96.591800f, 263.630000f, 179.256000f,  110.399000f, 184.785000f, 237.050000f,
            //96.591800f, 201.298000f, 230.220000f,  162.642000f, 174.870000f, 146.231000f,  96.591800f, 184.785000f, 137.609000f,  215.051000f, 93.966500f, 179.256000f,
            //137.873000f, 110.479000f, 144.655000f,  129.617000f, 184.785000f, 122.049000f,  96.591800f, 262.852000f, 171.000000f,  146.129000f, 168.273000f, 240.044000f,
            //113.104000f, 160.016000f, 246.016000f,  44.441300f, 193.041000f, 195.769000f,  47.054400f, 157.229000f, 179.256000f,  71.823100f, 250.835000f, 156.200000f,
            //129.617000f, 201.298000f, 135.345000f,  146.098000f, 127.017000f, 245.298000f,  61.130100f, 226.066000f, 204.025000f,  121.361000f, 163.348000f, 137.975000f,
            //113.186000f, 110.518000f, 245.291000f,  154.385000f, 95.879600f, 137.975000f,  170.898000f, 77.454000f, 211.685000f,  72.341800f, 226.066000f, 145.822000f,
            //183.151000f, 184.785000f, 204.025000f,  137.873000f, 102.223000f, 141.793000f,  203.923000f, 60.499200f, 171.000000f,  137.873000f, 217.810000f, 210.490000f,
            //203.923000f, 80.255600f, 220.537000f,  170.898000f, 102.223000f, 149.712000f,  195.667000f, 159.087000f, 220.537000f,  212.179000f, 68.223800f, 153.314000f,
            //76.349500f, 102.223000f, 179.256000f,  150.330000f, 193.041000f, 154.488000f,  121.361000f, 100.379000f, 237.050000f,  146.129000f, 198.308000f, 162.744000f,
            //55.310600f, 201.298000f, 131.598000f,  39.905000f, 184.785000f, 179.256000f,  121.361000f, 152.354000f, 162.879000f,  187.036000f, 160.016000f, 187.512000f,
            //88.335600f, 118.735000f, 139.500000f,  200.865000f, 126.991000f, 187.512000f,  137.873000f, 81.320300f, 212.281000f,  162.642000f, 117.842000f, 154.488000f,
            //38.845300f, 176.862000f, 170.644000f,  88.335600f, 93.966500f, 222.135000f,  203.923000f, 69.197800f, 201.548000f,  220.487000f, 126.991000f, 220.390000f,
            //207.055000f, 143.504000f, 204.025000f,  122.571000f, 209.935000f, 153.910000f,  132.582000f, 69.197800f, 146.231000f,  46.145600f, 160.016000f, 179.256000f,
            //236.948000f, 83.804400f, 195.769000f,  85.586100f, 135.248000f, 121.463000f,  179.154000f, 60.351600f, 137.975000f,  37.556100f, 184.785000f, 171.000000f,
            //78.300100f, 184.175000f, 129.719000f,  96.591800f, 82.717100f, 154.488000f,  214.880000f, 69.197800f, 154.488000f,  162.642000f, 143.504000f, 159.338000f,
            //92.137800f, 85.710300f, 195.769000f,  63.566900f, 226.066000f, 205.883000f,  184.189000f, 110.479000f, 162.744000f,  79.760700f, 209.946000f, 220.678000f,
            //170.898000f, 126.991000f, 240.802000f,  187.410000f, 58.507600f, 171.000000f,  212.179000f, 97.908900f, 179.256000f,  63.566900f, 234.322000f, 147.511000f,
            //96.591800f, 221.484000f, 220.537000f,  121.361000f, 77.454000f, 205.210000f,  154.385000f, 110.479000f, 239.523000f,  69.923700f, 126.991000f, 104.950000f,
            //170.898000f, 143.504000f, 240.830000f,  121.361000f, 184.785000f, 125.828000f,  121.361000f, 102.223000f, 139.160000f,  209.499000f, 93.966500f, 171.000000f,
            //129.617000f, 110.479000f, 243.590000f,  187.410000f, 168.273000f, 183.864000f,  134.184000f, 209.554000f, 220.537000f,  137.873000f, 176.529000f, 123.826000f,
            //38.798200f, 193.041000f, 175.495000f,  212.179000f, 110.479000f, 200.662000f,  105.086000f, 151.760000f, 145.848000f,  63.568400f, 135.247000f, 96.693500f,
            //126.462000f, 201.298000f, 228.794000f,  203.923000f, 62.640900f, 187.512000f,  59.574200f, 126.991000f, 204.025000f,  195.667000f, 59.032700f, 162.744000f,
            //135.944000f, 234.322000f, 187.512000f,  221.860000f, 118.735000f, 220.537000f,  212.179000f, 143.504000f, 210.332000f,  141.395000f, 226.066000f, 195.769000f,
            //63.566900f, 176.529000f, 108.552000f,  63.566900f, 143.504000f, 94.364100f,  146.129000f, 106.543000f, 237.050000f,  137.873000f, 102.223000f, 235.196000f,
            //129.118000f, 69.132200f, 162.744000f,  129.617000f, 176.529000f, 122.658000f,  154.385000f, 90.773900f, 129.719000f,  195.667000f, 81.423800f, 220.537000f,
            //176.876000f, 102.223000f, 154.488000f,  187.410000f, 71.500400f, 204.025000f,  99.635100f, 151.760000f, 137.975000f,  40.718600f, 176.529000f, 179.256000f,
            //47.054400f, 193.041000f, 201.307000f,  188.001000f, 160.161000f, 228.794000f,  96.591800f, 157.609000f, 245.306000f,  96.591800f, 84.016100f, 137.975000f,
            //170.898000f, 126.991000f, 160.746000f,  203.923000f, 118.735000f, 238.286000f,  195.667000f, 69.197800f, 140.528000f,  55.444800f, 127.154000f, 113.206000f,
            //104.848000f, 184.785000f, 136.487000f,  179.154000f, 81.155900f, 137.975000f,  121.361000f, 70.634500f, 154.488000f,  47.137700f, 167.515000f, 105.312000f,
            //59.605400f, 160.016000f, 220.537000f,  121.361000f, 193.041000f, 234.305000f,  228.692000f, 110.479000f, 226.716000f,  179.154000f, 99.244500f, 154.488000f,
            //38.798200f, 184.785000f, 154.557000f,  47.054400f, 168.273000f, 198.592000f,  183.038000f, 184.785000f, 187.512000f,  170.898000f, 73.760300f, 204.025000f,
            //137.873000f, 126.991000f, 152.048000f,  195.667000f, 143.504000f, 191.725000f,  63.566900f, 242.731000f, 195.933000f,  113.104000f, 212.915000f, 154.488000f,
            //63.566900f, 232.354000f, 146.231000f,  121.361000f, 82.293400f, 137.975000f,  41.304700f, 168.273000f, 171.000000f,  96.591800f, 135.248000f, 250.835000f,
            //179.154000f, 60.941500f, 137.068000f,  83.867400f, 226.066000f, 154.488000f,  55.686900f, 135.652000f, 154.488000f,  203.923000f, 159.839000f, 220.643000f,
            //121.361000f, 151.760000f, 248.277000f,  154.385000f, 191.305000f, 154.488000f,  129.617000f, 126.991000f, 148.737000f,  226.675000f, 93.966500f, 212.281000f,
            //180.984000f, 160.016000f, 204.025000f,  146.129000f, 135.248000f, 247.435000f,  137.725000f, 86.120400f, 129.457000f,  228.692000f, 73.554800f, 212.281000f,
            //229.806000f, 110.479000f, 228.794000f,  96.591800f, 193.041000f, 142.774000f,  63.566900f, 118.665000f, 202.796000f,  137.873000f, 110.479000f, 240.814000f,
            //187.410000f, 160.016000f, 217.543000f,  187.410000f, 57.474900f, 162.744000f,  187.410000f, 75.984500f, 212.281000f,  113.104000f, 251.521000f, 170.663000f,
            //187.410000f, 76.004900f, 137.975000f,  129.617000f, 71.982200f, 187.512000f,  121.361000f, 229.370000f, 162.744000f,  176.192000f, 201.298000f, 195.769000f,
            //212.179000f, 93.966500f, 234.753000f,  168.146000f, 193.041000f, 171.000000f,  162.642000f, 126.991000f, 242.232000f,  144.641000f, 184.785000f, 129.719000f,
            //129.617000f, 85.710300f, 136.616000f,  192.100000f, 69.197800f, 137.975000f,  136.297000f, 135.248000f, 154.488000f,  137.873000f, 168.273000f, 131.232000f,
            //71.823100f, 242.579000f, 148.918000f,  162.642000f, 193.041000f, 222.702000f,  231.063000f, 85.710300f, 195.769000f,  100.295000f, 160.016000f, 245.306000f,
            //187.410000f, 135.248000f, 237.413000f,  104.848000f, 113.905000f, 137.975000f,  39.545400f, 193.041000f, 154.488000f,  170.898000f, 118.735000f, 240.812000f,
            //187.410000f, 151.760000f, 235.849000f,  198.166000f, 135.248000f, 187.512000f,  187.410000f, 57.866300f, 146.231000f,  165.098000f, 102.223000f, 146.231000f,
            //220.435000f, 69.197800f, 199.814000f,  146.129000f, 163.535000f, 146.231000f,  162.642000f, 209.260000f, 195.769000f,  220.435000f, 98.031300f, 237.050000f,
            //154.385000f, 143.504000f, 159.037000f,  220.435000f, 69.197800f, 158.853000f,  69.776600f, 135.248000f, 228.794000f,  184.179000f, 176.529000f, 212.281000f,
            //170.898000f, 135.248000f, 240.186000f,  104.848000f, 76.749000f, 171.000000f,  53.031600f, 226.066000f, 195.769000f,  63.587200f, 118.782000f, 179.256000f,
            //203.923000f, 149.452000f, 237.050000f,  195.667000f, 112.724000f, 171.000000f,  142.851000f, 126.991000f, 154.488000f,  223.041000f, 102.223000f, 212.281000f,
            //185.636000f, 151.760000f, 187.512000f,  88.335600f, 259.091000f, 188.538000f,  71.823100f, 108.024000f, 171.000000f,  47.054400f, 150.915000f, 104.950000f,
            //113.104000f, 257.213000f, 179.256000f,  121.361000f, 74.255500f, 195.769000f,  154.190000f, 168.132000f, 237.004000f,  179.139000f, 193.009000f, 203.971000f,
            //129.617000f, 69.197800f, 167.327000f,  195.667000f, 118.735000f, 238.831000f,  88.257400f, 160.016000f, 121.539000f,  51.190600f, 143.504000f, 162.744000f,
            //104.848000f, 226.066000f, 217.277000f,  195.667000f, 146.836000f, 195.769000f,  71.823100f, 188.280000f, 129.719000f,  88.335600f, 128.397000f, 245.306000f,
            //96.591800f, 176.529000f, 132.724000f,  109.266000f, 160.016000f, 146.231000f,  96.591800f, 251.578000f, 196.624000f,  69.908400f, 110.479000f, 171.000000f,
            //187.410000f, 69.197800f, 134.652000f,  228.692000f, 85.710300f, 232.180000f,  137.873000f, 95.146400f, 227.371000f,  170.898000f, 93.966500f, 229.195000f,
            //113.104000f, 73.093000f, 171.000000f,  226.032000f, 118.735000f, 228.794000f,  93.777300f, 85.710300f, 204.025000f,  176.282000f, 168.273000f, 171.000000f,
            //154.385000f, 149.982000f, 245.306000f,  129.617000f, 126.991000f, 250.209000f,  96.591800f, 226.066000f, 217.124000f,  179.154000f, 85.710300f, 223.703000f,
            //104.848000f, 162.000000f, 245.306000f,  179.154000f, 88.417200f, 146.231000f,  187.410000f, 110.479000f, 164.693000f,  60.182600f, 126.991000f, 162.744000f,
            //63.667100f, 242.405000f, 154.488000f,  63.705100f, 118.960000f, 137.975000f,  162.642000f, 184.785000f, 229.812000f,  228.692000f, 86.977800f, 195.769000f,
            //137.873000f, 143.504000f, 157.978000f,  154.385000f, 135.248000f, 157.668000f,  88.335600f, 135.248000f, 245.999000f,  154.385000f, 176.529000f, 235.116000f,
            //195.456000f, 77.454000f, 146.231000f,  65.023100f, 160.016000f, 95.996400f,  121.361000f, 234.322000f, 205.244000f,  55.458000f, 127.222000f, 104.950000f,
            //187.410000f, 161.332000f, 220.537000f,  129.617000f, 151.760000f, 160.927000f,  113.104000f, 86.066800f, 220.154000f,  43.628100f, 176.529000f, 129.719000f,
            //154.385000f, 94.200300f, 228.616000f,  212.179000f, 155.979000f, 220.537000f,  195.667000f, 70.741300f, 204.025000f,  121.361000f, 85.710300f, 137.130000f,
            //129.617000f, 193.041000f, 233.499000f,  88.138200f, 234.717000f, 154.488000f,  113.104000f, 76.287000f, 195.769000f,  41.829900f, 168.273000f, 146.231000f,
            //63.566900f, 209.634000f, 212.307000f,  213.963000f, 102.223000f, 237.050000f,  43.318400f, 160.016000f, 162.744000f,  146.129000f, 110.479000f, 147.839000f,
            //104.848000f, 160.016000f, 141.000000f,  113.104000f, 110.479000f, 136.671000f,  71.823100f, 220.084000f, 212.281000f,  113.104000f, 78.756700f, 204.025000f,
            //55.310600f, 184.785000f, 114.739000f,  47.054400f, 201.298000f, 137.439000f,  178.394000f, 118.735000f, 162.744000f,  203.923000f, 160.016000f, 231.556000f,
            //55.310600f, 231.432000f, 154.488000f,  170.898000f, 168.273000f, 233.088000f,  113.104000f, 102.223000f, 239.715000f,  88.335600f, 242.579000f, 205.183000f,
            //88.742600f, 93.966500f, 129.918000f,  146.129000f, 209.554000f, 174.373000f,  113.104000f, 242.579000f, 166.236000f,  170.898000f, 168.273000f, 165.800000f,
            //81.584500f, 118.735000f, 237.050000f,  170.898000f, 101.826000f, 237.454000f,  162.642000f, 168.273000f, 234.904000f,  82.732900f, 93.966500f, 171.000000f,
            //161.429000f, 85.710300f, 129.719000f,  71.823100f, 130.063000f, 104.950000f,  154.385000f, 209.554000f, 182.818000f,  121.361000f, 118.735000f, 249.694000f,
            //232.598000f, 85.710300f, 212.281000f,  63.566900f, 176.529000f, 223.111000f,  220.435000f, 102.223000f, 237.688000f,  155.518000f, 168.273000f, 146.231000f,
            //157.235000f, 143.504000f, 245.306000f,  88.335600f, 88.526400f, 171.000000f,  88.335600f, 193.041000f, 141.801000f,  96.591800f, 86.236400f, 212.281000f,
            //170.898000f, 60.941500f, 175.332000f,  129.617000f, 160.503000f, 139.154000f,  187.410000f, 126.991000f, 172.027000f,  228.692000f, 69.197800f, 166.432000f,
            //54.605800f, 184.785000f, 213.080000f,  137.873000f, 193.041000f, 231.873000f,  88.335600f, 160.016000f, 242.233000f,  154.385000f, 61.290000f, 169.492000f,
            //88.335600f, 263.127000f, 171.000000f,  71.823100f, 112.031000f, 220.537000f,  184.435000f, 77.454000f, 137.975000f,  78.420400f, 160.016000f, 237.861000f,
            //38.798200f, 173.048000f, 162.744000f,  129.617000f, 113.528000f, 245.306000f,  137.873000f, 77.454000f, 125.935000f,  71.823100f, 107.479000f, 187.512000f,
            //45.867100f, 168.273000f, 195.769000f,  231.209000f, 93.966500f, 220.537000f,  179.154000f, 143.504000f, 238.855000f,  76.434700f, 209.554000f, 146.231000f,
            //113.104000f, 217.810000f, 222.494000f,  60.703500f, 143.504000f, 220.537000f,  154.385000f, 208.171000f, 179.256000f,  224.119000f, 135.248000f, 228.794000f,
            //171.115000f, 69.240300f, 195.685000f,  156.575000f, 209.554000f, 204.025000f,  221.217000f, 135.248000f, 237.789000f,  204.208000f, 68.422800f, 146.066000f,
            //146.129000f, 201.298000f, 165.678000f,  121.361000f, 206.681000f, 146.231000f,  47.054400f, 160.016000f, 101.314000f,  104.848000f, 143.504000f, 147.585000f,
            //198.986000f, 118.735000f, 179.256000f,  154.385000f, 162.781000f, 154.488000f,  46.607300f, 160.016000f, 187.512000f,  162.642000f, 79.293800f, 212.281000f,
            //179.154000f, 188.125000f, 212.281000f,  96.591800f, 85.710300f, 210.461000f,  146.129000f, 96.438800f, 137.975000f,  38.940800f, 176.529000f, 154.488000f,
            //80.079300f, 143.504000f, 239.033000f,  43.085100f, 160.016000f, 154.488000f,  121.361000f, 202.816000f, 228.794000f,  187.410000f, 77.454000f, 140.718000f,
            //212.225000f, 77.476000f, 154.468000f,  146.129000f, 206.954000f, 171.000000f,  113.104000f, 220.515000f, 220.537000f,  79.716800f, 126.991000f, 237.334000f,
            //195.667000f, 126.991000f, 238.589000f,  220.435000f, 74.263000f, 212.281000f,  179.154000f, 58.497100f, 171.000000f,  137.873000f, 227.996000f, 179.256000f,
            //71.823100f, 135.248000f, 101.702000f,  113.104000f, 162.049000f, 146.231000f,  179.154000f, 126.991000f, 165.823000f,  138.952000f, 226.066000f, 179.256000f,
            //88.335600f, 231.559000f, 212.281000f,  195.667000f, 84.471900f, 154.488000f,  44.661100f, 209.554000f, 187.512000f,  96.591800f, 177.162000f, 237.270000f,
            //80.079300f, 163.071000f, 113.206000f,  203.923000f, 93.966500f, 166.040000f,  84.828600f, 201.298000f, 146.231000f,  154.385000f, 76.433300f, 204.025000f,
            //80.079300f, 193.041000f, 229.301000f,  137.873000f, 104.396000f, 237.050000f,  146.129000f, 118.735000f, 241.844000f,  88.335600f, 113.345000f, 137.975000f,
            //186.203000f, 126.991000f, 171.000000f,  228.692000f, 93.966500f, 215.707000f,  76.028300f, 135.248000f, 104.950000f,  119.610000f, 217.810000f, 220.537000f,
            //80.079300f, 201.298000f, 225.467000f,  162.642000f, 160.016000f, 238.372000f,  71.823100f, 143.504000f, 233.298000f,  129.617000f, 77.454000f, 136.454000f,
            //162.642000f, 63.745700f, 179.256000f,  184.007000f, 151.760000f, 237.050000f,  44.121500f, 184.785000f, 195.769000f,  48.488900f, 151.760000f, 187.512000f,
            //69.387200f, 110.479000f, 154.488000f,  57.295000f, 242.579000f, 179.256000f,  59.714800f, 151.760000f, 220.537000f,  212.468000f, 102.223000f, 187.512000f,
            //121.361000f, 217.320000f, 220.106000f,  221.315000f, 118.735000f, 237.223000f,  170.898000f, 190.021000f, 171.000000f,  104.848000f, 80.817500f, 204.025000f,
            //64.818000f, 160.016000f, 227.191000f,  59.503100f, 242.579000f, 162.744000f,  104.848000f, 118.735000f, 247.440000f,  110.260000f, 234.322000f, 162.744000f,
            //104.848000f, 251.502000f, 196.485000f,  162.642000f, 151.760000f, 162.460000f,  154.385000f, 189.992000f, 146.231000f,  236.948000f, 70.106800f, 195.769000f,
            //129.617000f, 209.554000f, 160.767000f,  104.848000f, 261.380000f, 179.256000f,  47.054400f, 209.554000f, 144.056000f,  162.642000f, 207.652000f, 179.256000f,
            //146.129000f, 118.735000f, 152.488000f,  146.129000f, 143.504000f, 159.554000f,  220.435000f, 79.813600f, 162.744000f,  162.642000f, 68.713500f, 129.236000f,
            //162.642000f, 195.028000f, 220.537000f,  55.310600f, 132.420000f, 121.463000f,  129.617000f, 234.322000f, 200.400000f,  137.873000f, 65.908700f, 154.488000f,
            //187.465000f, 143.089000f, 237.038000f,  228.094000f, 85.440700f, 187.512000f,  80.079300f, 118.735000f, 131.645000f,  212.179000f, 102.439000f, 186.969000f,
            //104.848000f, 160.016000f, 245.885000f,  103.615000f, 77.454000f, 171.000000f,  80.079300f, 100.522000f, 220.537000f,  88.335600f, 88.519000f, 187.512000f,
            //187.410000f, 77.454000f, 214.864000f,  113.104000f, 201.298000f, 230.400000f,  63.821700f, 119.138000f, 171.000000f,  179.154000f, 92.606800f, 228.794000f,
            //96.591800f, 93.966500f, 126.980000f,  187.410000f, 59.158600f, 179.256000f,  63.566900f, 118.735000f, 208.450000f,  96.591800f, 126.991000f, 249.283000f,
            //162.642000f, 62.144000f, 137.975000f,  212.179000f, 110.479000f, 236.710000f,  141.772000f, 110.479000f, 146.231000f,  145.567000f, 160.092000f, 154.488000f,
            //55.310600f, 238.312000f, 187.512000f,  163.024000f, 176.529000f, 145.682000f,  71.823100f, 107.810000f, 179.256000f,  113.104000f, 209.554000f, 226.657000f,
            //88.335600f, 126.991000f, 135.683000f,  162.642000f, 187.097000f, 154.488000f,  187.410000f, 90.724700f, 154.488000f,  154.385000f, 151.760000f, 161.355000f,
            //154.385000f, 59.513800f, 154.488000f,  51.004000f, 143.504000f, 129.719000f,  104.848000f, 234.322000f, 162.304000f,  212.179000f, 160.016000f, 223.047000f,
            //71.823100f, 250.835000f, 192.945000f,  43.344500f, 201.298000f, 146.231000f,  71.823100f, 151.760000f, 234.537000f,  113.104000f, 118.735000f, 248.849000f,
            //42.835300f, 209.554000f, 179.256000f,  162.642000f, 71.123500f, 195.769000f,  146.129000f, 107.320000f, 146.231000f,  88.335600f, 89.056000f, 137.975000f,
            //49.953200f, 201.298000f, 204.025000f,  146.129000f, 151.760000f, 160.483000f,  146.129000f, 195.793000f, 154.488000f,  63.566900f, 126.991000f, 101.676000f,
            //129.617000f, 80.779000f, 212.281000f,  55.310600f, 133.990000f, 137.975000f,  130.447000f, 69.273100f, 171.000000f,  129.617000f, 186.060000f, 237.050000f,
            //77.116700f, 143.504000f, 237.050000f,  179.154000f, 69.197800f, 198.685000f,  162.642000f, 58.124400f, 162.744000f,  234.859000f, 93.966500f, 228.794000f,
            //64.174100f, 126.991000f, 219.756000f,  50.065600f, 217.810000f, 146.231000f,  203.923000f, 126.991000f, 239.260000f,  179.154000f, 76.692400f, 212.281000f,
            //48.950900f, 176.529000f, 204.025000f,  80.079300f, 209.554000f, 148.488000f,  187.410000f, 61.303200f, 186.073000f,  212.179000f, 143.504000f, 239.475000f,
            //96.591800f, 82.712800f, 146.231000f,  96.591800f, 85.710300f, 134.577000f,  236.948000f, 76.603600f, 179.256000f,  195.667000f, 124.326000f, 179.256000f,
            //39.013600f, 200.663000f, 162.744000f,  170.898000f, 151.760000f, 240.306000f,  195.667000f, 156.589000f, 212.281000f,  104.848000f, 238.601000f, 162.744000f,
            //40.200300f, 168.273000f, 162.744000f,  113.104000f, 239.676000f, 204.025000f,  210.731000f, 85.051600f, 162.744000f,  71.823100f, 178.959000f, 228.794000f,
            //207.132000f, 60.941500f, 171.000000f,  223.000000f, 93.966500f, 204.025000f,  137.873000f, 118.735000f, 148.536000f,  80.079300f, 135.248000f, 109.990000f,
            //71.823100f, 179.594000f, 121.463000f,  125.735000f, 217.810000f, 162.744000f,  96.591800f, 168.273000f, 241.497000f,  217.782000f, 118.735000f, 212.281000f,
            //71.823100f, 110.479000f, 137.327000f,  93.847700f, 176.529000f, 237.050000f,  129.617000f, 85.710300f, 218.667000f,  80.079300f, 261.522000f, 171.000000f,
            //217.902000f, 93.966500f, 187.512000f,  104.974000f, 259.148000f, 170.878000f,  181.151000f, 193.041000f, 195.769000f,  47.054400f, 221.560000f, 171.000000f,
            //195.667000f, 62.317600f, 187.512000f,  146.129000f, 68.701800f, 128.787000f,  228.692000f, 82.408300f, 228.794000f,  88.335600f, 259.091000f, 159.988000f,
            //129.617000f, 200.839000f, 228.469000f,  195.667000f, 135.248000f, 238.442000f,  195.667000f, 144.973000f, 237.050000f,  71.823100f, 118.735000f, 225.490000f,
            //187.410000f, 81.673500f, 220.537000f,  148.370000f, 209.554000f, 212.281000f,  88.335600f, 124.017000f, 137.975000f,  162.642000f, 77.454000f, 208.804000f,
            //150.266000f, 60.941500f, 162.744000f,  187.410000f, 118.735000f, 168.697000f,  154.385000f, 201.298000f, 167.728000f,  113.104000f, 72.965300f, 154.488000f,
            //187.695000f, 168.085000f, 187.512000f,  68.615100f, 110.479000f, 204.025000f,  115.018000f, 168.273000f, 137.975000f,  212.179000f, 66.432000f, 195.769000f,
            //113.104000f, 72.878200f, 162.744000f,  88.534100f, 126.991000f, 245.176000f,  220.435000f, 64.438700f, 171.000000f,  137.873000f, 66.400100f, 146.231000f,
            //170.898000f, 59.791100f, 171.000000f,  88.335600f, 209.554000f, 225.244000f,  179.154000f, 93.966500f, 229.893000f,  121.361000f, 217.810000f, 160.042000f,
            //129.617000f, 139.247000f, 154.488000f,  59.004800f, 126.991000f, 187.512000f,  96.591800f, 259.259000f, 162.343000f,  154.385000f, 63.519600f, 137.975000f,
            //212.179000f, 63.301800f, 187.512000f,  80.079300f, 97.304600f, 179.256000f,  129.617000f, 196.910000f, 129.719000f,  162.642000f, 104.128000f, 146.231000f,
            //171.029000f, 176.529000f, 162.390000f,  84.221300f, 102.223000f, 228.794000f,  71.823100f, 124.791000f, 113.206000f,  71.823100f, 162.920000f, 104.950000f,
            //155.636000f, 60.941600f, 146.231000f,  220.260000f, 93.966500f, 196.443000f,  96.591800f, 184.785000f, 234.483000f,  113.104000f, 151.760000f, 157.567000f,
            //129.617000f, 70.254400f, 179.256000f,  174.924000f, 193.041000f, 212.281000f,  195.667000f, 102.223000f, 165.535000f,  125.599000f, 242.579000f, 195.769000f,
            //121.361000f, 168.273000f, 132.456000f,  183.564000f, 160.016000f, 171.000000f,  88.335600f, 89.145500f, 204.025000f,  214.652000f, 151.760000f, 220.537000f,
            //41.776200f, 193.041000f, 146.231000f,  170.898000f, 86.788300f, 137.975000f,  179.154000f, 185.545000f, 179.256000f,  129.617000f, 118.735000f, 248.051000f,
            //170.898000f, 190.462000f, 220.537000f,  53.181100f, 234.322000f, 171.000000f,  195.667000f, 69.197800f, 201.453000f,  146.129000f, 81.800500f, 212.281000f,
            //212.179000f, 135.248000f, 206.115000f,  224.866000f, 69.197800f, 162.744000f,  174.613000f, 60.941500f, 179.256000f,  58.466600f, 242.579000f, 187.512000f,
            //80.079300f, 151.760000f, 239.029000f,  137.873000f, 229.891000f, 195.769000f,  45.459800f, 168.273000f, 113.206000f,  220.877000f, 143.504000f, 237.253000f,
            //121.361000f, 201.298000f, 229.761000f,  154.385000f, 126.991000f, 157.088000f,  129.911000f, 119.092000f, 146.137000f,  91.018100f, 143.504000f, 129.719000f,
            //188.061000f, 143.504000f, 179.256000f,  162.642000f, 69.197800f, 192.274000f,  174.084000f, 176.529000f, 228.794000f,  184.217000f, 184.785000f, 195.769000f,
            //56.919900f, 226.066000f, 146.231000f,  121.361000f, 143.504000f, 250.170000f,  185.678000f, 168.273000f, 179.256000f,  179.154000f, 182.038000f, 220.537000f,
            //54.530300f, 210.067000f, 137.975000f,  187.443000f, 151.780000f, 195.691000f,  179.154000f, 77.454000f, 213.753000f,  154.385000f, 64.494200f, 179.256000f,
            //146.129000f, 77.235100f, 204.606000f,  72.391000f, 217.810000f, 145.894000f,  104.848000f, 86.732900f, 220.537000f,  45.662600f, 217.810000f, 162.744000f,
            //96.261000f, 160.016000f, 130.073000f,  170.247000f, 77.454000f, 130.067000f,  146.129000f, 191.477000f, 137.975000f,  76.120900f, 102.223000f, 146.231000f,
            //154.143000f, 176.529000f, 138.162000f,  113.104000f, 226.066000f, 215.642000f,  40.741600f, 201.298000f, 179.256000f,  212.179000f, 85.710300f, 229.239000f,
            //137.873000f, 176.529000f, 239.950000f,  137.873000f, 197.491000f, 228.794000f,  162.642000f, 206.499000f, 204.025000f,  45.541900f, 201.298000f, 195.769000f,
            //71.823100f, 257.838000f, 179.256000f,  104.848000f, 78.936400f, 195.769000f,  80.079300f, 96.217100f, 204.025000f,  162.642000f, 102.676000f, 236.752000f,
            //213.521000f, 143.504000f, 212.281000f,  124.667000f, 110.479000f, 245.306000f,  80.079300f, 97.456200f, 162.744000f,  51.954300f, 135.248000f, 113.206000f,
            //154.385000f, 190.407000f, 228.794000f, 146.129000f, 64.644000f, 171.000000f };

            //void main(int /*argc*/,const char * /*argv*/)
            //{

            //  double matrix[16];
            //  double sides[3];
            //  unsigned int PCOUNT = sizeof(points)/(sizeof(double)*3);
            //  bf_computeBestFitOBB(PCOUNT,points,sizeof(double)*3,sides,matrix,true);

            //  printf("Best Fit OBB dimensions: %0.9f,%0.9f,%0.9f\r\n", sides[0], sides[1], sides[2] );
            //  printf("Best Fit OBB matrix is:\r\n");
            //  printf("Row1:  %0.9f, %09f, %0.9f, %0.9f \r\n", matrix[0], matrix[1], matrix[2], matrix[3] );
            //  printf("Row2:  %0.9f, %09f, %0.9f, %0.9f \r\n", matrix[4], matrix[5], matrix[6], matrix[7] );
            //  printf("Row3:  %0.9f, %09f, %0.9f, %0.9f \r\n", matrix[8], matrix[9], matrix[10], matrix[11] );
            //  printf("Row4:  %0.9f, %09f, %0.9f, %0.9f \r\n", matrix[12], matrix[13], matrix[14], matrix[15] );


            //}

            //#endif
            #endregion

            #endregion

            #endregion

            public static ITriangle GetAveragePlane(Point3D[] points, bool matchPolyNormalDirection = false)
            {
                if (points.Length < 3)
                {
                    return null;
                }

                // The plane will go through this center point
                Point3D center = Math3D.GetCenter(points);

                // Get a bunch of sample up vectors
                Vector3D[] upVectors = GetAveragePlane_UpVectors(points, matchPolyNormalDirection);
                if (upVectors.Length == 0)
                {
                    return null;
                }

                // Average them together to get a single normal
                //TODO: May want to create a small loop that tries to minimize error
                Vector3D avgNormal = GetAveragePlane_Normal(upVectors);

                // Exit Function
                return Math3D.GetPlane(center, avgNormal);
            }

            private static Vector3D[] ORIG_GetAveragePlane_UpVectors(Point3D[] points)
            {
                const int MAX = 100;

                //TODO: Limit the number returned (try to get vectors from points as far away from each other as possible)
                //Although it may be more expensive to find a few "best" points, than just brute force getting them all (it's not that expensive to average them together)

                if (points.Length < 3)
                {
                    return new Vector3D[0];
                }

                List<Vector3D> retVal = new List<Vector3D>();

                int perBaseCount = points.Length - 2;
                int totalCount = points.Length * perBaseCount;

                if (totalCount < MAX)
                {
                    // There aren't enough to exceed max, just use all permutations
                    for (int cntr = 0; cntr < totalCount; cntr++)
                    {
                        ORIG_GetAveragePlane_UpVectors_Vector(retVal, cntr, points, perBaseCount);
                    }
                }
                else
                {
                    // There would be too many triangles generated, so choose a random sample
                    foreach (int cntr in UtilityCore.RandomRange(0, totalCount))     // can't limit the count here, because some points may be colinear (so no up vector generated for some sets of points)
                    {
                        ORIG_GetAveragePlane_UpVectors_Vector(retVal, cntr, points, perBaseCount);
                        if (retVal.Count >= MAX)
                        {
                            break;
                        }
                    }
                }

                // Make sure they are all pointing in the same direction
                for (int cntr = 1; cntr < retVal.Count; cntr++)
                {
                    if (Vector3D.DotProduct(retVal[0], retVal[cntr]) < 0)
                    {
                        retVal[cntr] = retVal[cntr] * -1d;
                    }
                }

                return retVal.ToArray();
            }
            private static void ORIG_GetAveragePlane_UpVectors_Vector(List<Vector3D> returnVectors, int cntr, Point3D[] points, int perBaseCount)
            {
                int baseIndex = cntr / perBaseCount;
                int index1 = baseIndex + (cntr - (baseIndex * perBaseCount) + 1);
                int index2 = index1 + 1;

                if (index1 >= points.Length)
                {
                    index1 -= points.Length;
                }

                if (index2 >= points.Length)
                {
                    index2 -= points.Length;
                }

                Vector3D cross = Vector3D.CrossProduct(points[index1] - points[baseIndex], points[index2] - points[baseIndex]);
                if (!Math3D.IsInvalid(cross) && !Math3D.IsNearZero(cross))        // there may be colinear points
                {
                    returnVectors.Add(cross);
                }
            }

            /// <summary>
            /// This chooses a bunch of random triangles out of the polygon passed in, and returns their normals (all pointing the
            /// same direction)
            /// </summary>
            /// <remarks>
            /// This limits the number of returned vectors to 100.  Here is a small table of the number of triangles based
            /// on the number of points (it's somewhere below count^3)
            /// 
            /// 3 - 1
            /// 4 - 4
            /// 5 - 10
            /// 6 - 20
            /// 7 - 35
            /// 8 - 56
            /// 9 - 84
            /// 10 - 120
            /// 11 - 165
            /// 12 - 220
            /// 13 - 286
            /// 14 - 364
            /// 15 - 455
            /// 16 - 560
            /// 17 - 680
            /// 18 - 816
            /// 19 - 969
            /// 20 - 1140
            /// 21 - 1330
            /// 22 - 1540
            /// </remarks>
            /// <param name="matchPolyNormalDirection">
            /// True = They will point in the direction of the polygon's normal (only makes sense if the points represent a polygon, and that polygon is convex)
            /// False = The direction of the vectors returned is arbitrary (they will all point in the same direction, but it's random which direction that will be)
            /// </param>
            private static Vector3D[] GetAveragePlane_UpVectors(Point3D[] points, bool matchPolyNormalDirection = false)
            {
                if (points.Length < 3)
                {
                    return new Vector3D[0];
                }

                Vector3D[] retVal;
                if (points.Length < 15)      //see the table in the remarks.  Even though 13 makes 364 triangles, it would be inneficient to randomly choose triangles, and throw out already attempted ones (I was looking for at least a 1:4 ratio - didn't do performance testing, just feels right)
                {
                    // Do them all
                    retVal = GetAveragePlane_UpVectors_All(points);
                }
                else
                {
                    // Randomly choose 100 points
                    retVal = GetAveragePlane_UpVectors_Sample(points, 100);
                }

                // Make sure they are all pointing in the same direction
                GetAveragePlane_SameDirection(retVal, points, matchPolyNormalDirection);

                return retVal.ToArray();
            }
            private static Vector3D[] GetAveragePlane_UpVectors_All(Point3D[] points)
            {
                List<Vector3D> retVal = new List<Vector3D>();

                for (int a = 0; a < points.Length - 2; a++)
                {
                    for (int b = a + 1; b < points.Length - 1; b++)
                    {
                        for (int c = b + 1; c < points.Length; c++)
                        {
                            GetAveragePlane_UpVectors_Vector(retVal, a, b, c, points);
                        }
                    }
                }

                return retVal.ToArray();
            }
            private static Vector3D[] GetAveragePlane_UpVectors_Sample(Point3D[] points, int count)
            {
                List<Vector3D> retVal = new List<Vector3D>();
                SortedList<Tuple<int, int, int>, byte> used = new SortedList<Tuple<int, int, int>, byte>();     // the value doesn't mean anything, I just wanted to keep the keys sorted

                Random rand = StaticRandom.GetRandomForThread();
                int pointsLen = points.Length;      // not sure if there is a cost to hitting the length property, but this method would hit it a lot

                int infiniteLoopCntr1 = 0;
                int[] indices = new int[3];

                while (retVal.Count < count && infiniteLoopCntr1 < 40)
                {
                    int infiniteLoopCntr2 = 0;
                    Tuple<int, int, int> triangle = null;
                    while (infiniteLoopCntr2 < 1000)
                    {
                        infiniteLoopCntr2++;

                        indices[0] = rand.Next(pointsLen);
                        indices[1] = rand.Next(pointsLen);
                        indices[2] = rand.Next(pointsLen);

                        if (indices[0] == indices[1] || indices[0] == indices[2])
                        {
                            continue;
                        }

                        // Sort them
                        Array.Sort(indices);

                        triangle = Tuple.Create(indices[0], indices[1], indices[2]);

                        if (used.ContainsKey(triangle))
                        {
                            continue;
                        }

                        used.Add(triangle, 0);

                        break;
                    }

                    if (GetAveragePlane_UpVectors_Vector(retVal, triangle.Item1, triangle.Item2, triangle.Item3, points))
                    {
                        infiniteLoopCntr1 = 0;
                    }
                    else
                    {
                        infiniteLoopCntr1++;
                    }
                }

                return retVal.ToArray();
            }
            private static bool GetAveragePlane_UpVectors_Vector(List<Vector3D> returnVectors, int index1, int index2, int index3, Point3D[] points)
            {
                Vector3D cross = Vector3D.CrossProduct(points[index2] - points[index1], points[index3] - points[index1]);
                if (!Math3D.IsInvalid(cross) && !Math3D.IsNearZero(cross))        // there may be colinear points
                {
                    returnVectors.Add(cross);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            private static void GetAveragePlane_SameDirection(Vector3D[] vectors, Point3D[] points, bool matchPolyNormalDirection)
            {
                // Get the vector to compare with
                int start;
                Vector3D match;
                if (matchPolyNormalDirection)
                {
                    start = 0;
                    match = Math2D.GetPolygonNormal(points, PolygonNormalLength.DoesntMatter);
                }
                else
                {
                    start = 1;
                    match = vectors[0];
                }

                // Make sure all the vectors match that direction
                for (int cntr = start; cntr < vectors.Length; cntr++)
                {
                    if (Vector3D.DotProduct(vectors[cntr], match) < 0)
                    {
                        vectors[cntr] = vectors[cntr] * -1d;
                    }
                }
            }

            private static Vector3D GetAveragePlane_Normal(Vector3D[] upVectors)
            {
                // This is copied from Math3D.GetCenter

                if (upVectors == null)
                {
                    return new Vector3D(0, 0, 0);
                }

                double x = 0d;
                double y = 0d;
                double z = 0d;

                int length = 0;

                foreach (Vector3D vector in upVectors)
                {
                    x += vector.X;
                    y += vector.Y;
                    z += vector.Z;

                    length++;
                }

                if (length == 0)
                {
                    return new Vector3D(0, 0, 0);
                }

                double oneOverLen = 1d / Convert.ToDouble(length);

                return new Vector3D(x * oneOverLen, y * oneOverLen, z * oneOverLen);
            }

            public static Tuple<int, int, int>[] TestVectors(int count)
            {
                List<Tuple<int, int, int>> retVal = new List<Tuple<int, int, int>>();

                for (int a = 0; a < count - 2; a++)
                {
                    for (int b = a + 1; b < count - 1; b++)
                    {
                        for (int c = b + 1; c < count; c++)
                        {
                            retVal.Add(Tuple.Create(a, b, c));
                        }
                    }
                }

                return retVal.ToArray();
            }

            public static Tuple<Vector3D, Vector3D> TestAveragePlane_UpVectors(Point3D[] points)
            {
                if (points.Length < 3)
                {
                    throw new ApplicationException("no");
                }

                Vector3D[] vectors1 = GetAveragePlane_UpVectors_All(points);
                Vector3D[] vectors2 = GetAveragePlane_UpVectors_Sample(points, 100);

                GetAveragePlane_SameDirection(vectors1, points, false);
                GetAveragePlane_SameDirection(vectors2, points, false);

                Vector3D avgNormal1 = GetAveragePlane_Normal(vectors1);
                Vector3D avgNormal2 = GetAveragePlane_Normal(vectors2);

                if (Vector3D.DotProduct(avgNormal1, avgNormal2) < 0)
                {
                    avgNormal2 = avgNormal2 * -1;
                }

                return Tuple.Create(avgNormal1, avgNormal2);
            }
        }

        #endregion

        #region Declaration Section

        private const string _endPointS = "293335";
        private Color _endPointC = UtilityWPF.ColorFromHex(_endPointS);
        private Brush _endPointB = new SolidColorBrush(UtilityWPF.ColorFromHex(_endPointS));        // had to define _endPointS as a const, or the compiler chokes here

        private const string _mainLineS = "FFF8F8FF";
        private Color _mainLineC = UtilityWPF.ColorFromHex(_mainLineS);
        private Brush _mainLineB = new SolidColorBrush(UtilityWPF.ColorFromHex(_mainLineS));

        private const string _controlPointS = "8F6D77";
        private Color _controlPointC = UtilityWPF.ColorFromHex(_controlPointS);
        private Brush _controlPointB = new SolidColorBrush(UtilityWPF.ColorFromHex(_controlPointS));

        private const string _controlLineS = "B0DBA7B7";
        private Color _controlLineC = UtilityWPF.ColorFromHex(_controlLineS);
        private Brush _controlLineB = new SolidColorBrush(UtilityWPF.ColorFromHex(_controlLineS));

        private const string _otherPointS = "5C5B46";
        private Color _otherPointC = UtilityWPF.ColorFromHex(_otherPointS);
        private Brush _otherPointB = new SolidColorBrush(UtilityWPF.ColorFromHex(_otherPointS));

        private const string _otherLineS = "B0CECCA8";
        private Color _otherLineC = UtilityWPF.ColorFromHex(_otherLineS);
        private Brush _otherLineB = new SolidColorBrush(UtilityWPF.ColorFromHex(_otherLineS));

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackball = null;

        private List<Visual3D> _visuals = new List<Visual3D>();

        private Point3D[] _savedEnds = null;
        private object _savedAxe = null;

        #endregion

        #region Constructor

        public Curves()
        {
            InitializeComponent();

            // Camera Trackball
            _trackball = new TrackBallRoam(_camera);
            _trackball.EventSource = grdViewPort;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
            _trackball.AllowZoomOnMouseWheel = true;
            _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
            //_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);
        }

        #endregion

        #region Event Listeners

        private void btnSingleLine1Segment2D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double marginX = _canvas.ActualWidth * .05d;
                double marginY = _canvas.ActualHeight * .05d;
                Vector3D min = new Vector3D(marginX, marginY, 0);
                Vector3D max = new Vector3D(_canvas.ActualWidth - marginX, _canvas.ActualHeight - marginY, 0);

                // End points
                Point end1 = Math3D.GetRandomVector(min, max).ToPoint2D();
                Point end2 = Math3D.GetRandomVector(min, max).ToPoint2D();

                // Control points (usually only want 1 or 2.  more than that is just silly)
                Point[] control = Enumerable.Range(0, StaticRandom.Next(6)).
                    Select(o => Math3D.GetRandomVector(min, max).ToPoint2D()).
                    ToArray();

                // Calculate the beziers
                Point[] bezierPoints = Math3D.GetBezierSegment(100, end1.ToPoint3D(), control.Select(o => o.ToPoint3D()).ToArray(), end2.ToPoint3D()).Select(o => o.ToPoint2D()).ToArray();

                #region Draw

                PrepFor2D();

                // Main Line
                for (int cntr = 0; cntr < bezierPoints.Length - 1; cntr++)
                {
                    AddLine(bezierPoints[cntr], bezierPoints[cntr + 1], _mainLineB, 3);
                }

                if (chkShowDots.IsChecked.Value)
                {
                    foreach (Point point in bezierPoints)
                    {
                        AddDot(point, _mainLineB, 8);
                    }

                    // Control Lines
                    Point[] allPoints = UtilityCore.Iterate<Point>(end1, control, end2).ToArray();

                    for (int cntr = 1; cntr < allPoints.Length - 1; cntr++)
                    {
                        AddLine(allPoints[cntr], allPoints[cntr + 1], _controlLineB, 1);
                    }

                    for (int cntr = 0; cntr < control.Length; cntr++)
                    {
                        AddDot(control[cntr], _controlPointB);
                    }

                    // End Points
                    AddDot(end1, _endPointB);
                    AddDot(end2, _endPointB);
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnSingleLine1Segment3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double halfSize = 5;

                // End points
                Point3D end1 = Math3D.GetRandomVector(halfSize).ToPoint();
                Point3D end2 = Math3D.GetRandomVector(halfSize).ToPoint();

                // Control points (usually only want 1 or 2.  more than that is just silly)
                Point3D[] control = Enumerable.Range(0, StaticRandom.Next(6)).
                    Select(o => Math3D.GetRandomVector(halfSize).ToPoint()).
                    ToArray();

                // Calculate the beziers
                Point3D[] bezierPoints = Math3D.GetBezierSegment(100, end1, control, end2);

                #region Draw

                PrepFor3D();

                // Main Line
                for (int cntr = 0; cntr < bezierPoints.Length - 1; cntr++)
                {
                    AddLine(bezierPoints[cntr], bezierPoints[cntr + 1], _mainLineC, 2);
                }

                if (chkShowDots.IsChecked.Value)
                {
                    foreach (Point3D point in bezierPoints)
                    {
                        AddDot(point, _mainLineC, .05);
                    }

                    // Control Lines
                    Point3D[] allPoints = UtilityCore.Iterate<Point3D>(end1, control, end2).ToArray();

                    for (int cntr = 0; cntr < allPoints.Length - 1; cntr++)
                    {
                        AddLine(allPoints[cntr], allPoints[cntr + 1], _controlLineC);
                    }

                    for (int cntr = 0; cntr < control.Length; cntr++)
                    {
                        AddDot(control[cntr], _controlPointC);
                    }

                    // End Points
                    AddDot(end1, _endPointC);
                    AddDot(end2, _endPointC);
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSingleLine2Segments2D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double marginX = _canvas.ActualWidth * .05d;
                double marginY = _canvas.ActualHeight * .05d;
                Vector3D min = new Vector3D(marginX, marginY, 0);
                Vector3D max = new Vector3D(_canvas.ActualWidth - marginX, _canvas.ActualHeight - marginY, 0);

                // End points (1-2, 2-3)
                Point end1 = Math3D.GetRandomVector(min, max).ToPoint2D();
                Point end2 = Math3D.GetRandomVector(min, max).ToPoint2D();
                Point end3 = Math3D.GetRandomVector(min, max).ToPoint2D();
                _savedEnds = new[] { end1, end2, end3 }.Select(o => o.ToPoint3D()).ToArray();

                Test2Segments2D(end1, end2, end3);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void trkSingleLine2Segments2DPercent_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (_savedEnds == null || _savedEnds.Length != 3)
                {
                    return;
                }

                Test2Segments2D(_savedEnds[0].ToPoint2D(), _savedEnds[1].ToPoint2D(), _savedEnds[2].ToPoint2D());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSingleLineMultiSegments3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double halfSize = 5;

                _savedEnds = Enumerable.Range(0, 2 + StaticRandom.Next(8)).
                    Select(o => Math3D.GetRandomVector(halfSize).ToPoint()).
                    ToArray();

                TestMultiSegment3D(_savedEnds);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnPolygon3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Use a random voronoi to build random convex polygon
                Point[] points = Enumerable.Range(0, StaticRandom.Next(6, 13)).
                    Select(o => Math3D.GetRandomVector_Circular(4).ToPoint2D()).
                    ToArray();

                VoronoiResult2D voronoi = Math2D.CapVoronoiCircle(Math2D.GetVoronoi(points, true));

                points = voronoi.GetPolygon(StaticRandom.Next(voronoi.ControlPoints.Length), 1);

                _savedEnds = points.
                    Select(o => o.ToPoint3D()).
                    ToArray();

                TestMultiSegment3D(_savedEnds);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void trkSingleLineMultiSegments3DPercent_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (_savedEnds == null || _savedEnds.Length < 2)
                {
                    return;
                }

                TestMultiSegment3D(_savedEnds);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void chkIsClosed3D_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_savedEnds == null || _savedEnds.Length < 2)
                {
                    return;
                }

                TestMultiSegment3D(_savedEnds);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void chkShowDots_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_savedEnds == null || _savedEnds.Length < 2)
                {
                    return;
                }

                bool is3D = _viewport.Visibility == Visibility.Visible;

                if (!is3D && _savedEnds.Length == 3)
                {
                    Test2Segments2D(_savedEnds[0].ToPoint2D(), _savedEnds[1].ToPoint2D(), _savedEnds[2].ToPoint2D());
                }
                else
                {
                    TestMultiSegment3D(_savedEnds);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAxeSimple1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _savedAxe = new AxeSimple1();

                Axe_Checked(this, new RoutedEventArgs());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnAxeSimple2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _savedAxe = new Curves_AxeSimple2(false);

                Axe_Checked(this, new RoutedEventArgs());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Axe_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_savedAxe == null)
                {
                    return;
                }

                if (_savedAxe is AxeSimple1)
                {
                    TestAxeSimple1((AxeSimple1)_savedAxe);
                }
                else if (_savedAxe is Curves_AxeSimple2)
                {
                    TestAxeSimple2((Curves_AxeSimple2)_savedAxe);
                }
                else
                {
                    throw new ApplicationException("Unknown axe type: " + _savedAxe.GetType().ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAvgPlane_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Curves_AxeSimple2 arg = new Curves_AxeSimple2(false);
                Curves_AxeSimple2[] argLevels = new Curves_AxeSimple2[5];

                argLevels[2] = arg;     // Edge
                #region Middle

                argLevels[1] = UtilityCore.Clone(arg);

                // Right
                argLevels[1].EndTR = new Point3D(argLevels[1].EndTR.X * .8, argLevels[1].EndTR.Y * .9, .15);

                Vector3D offsetTR = new Point3D(argLevels[1].EndTR.X, argLevels[1].EndTR.Y, 0) - arg.EndTR;
                Quaternion angleTR = Math3D.GetRotation((arg.EndTL - arg.EndTR), offsetTR);

                Vector3D rotated = (arg.EndBL_1 - arg.EndBR).ToUnit() * offsetTR.Length;
                rotated = rotated.GetRotatedVector(angleTR.Axis, angleTR.Angle * -1.3);

                argLevels[1].EndBR = new Point3D(argLevels[1].EndBR.X + rotated.X, argLevels[1].EndBR.Y + rotated.Y, .15);      // can't just use percents of coordinates.  Instead use the same offset angle,distance that top left had

                // Left
                argLevels[1].EndTL = new Point3D(argLevels[1].EndTL.X, argLevels[1].EndTL.Y * .95, .3);

                if (argLevels[1].EndBL_2 == null)
                {
                    argLevels[1].EndBL_1 = new Point3D(argLevels[1].EndBL_1.X, argLevels[1].EndBL_1.Y * .9, .3);
                }
                else
                {
                    Vector3D offsetBL1 = arg.EndBR - arg.EndBL_1;
                    double lengthBL1 = (new Point3D(argLevels[1].EndBR.X, argLevels[1].EndBR.Y, 0) - arg.EndBR).Length;
                    offsetBL1 = offsetBL1.ToUnit() * (offsetBL1.Length - lengthBL1);

                    argLevels[1].EndBL_1 = argLevels[1].EndBR - offsetBL1;
                    argLevels[1].EndBL_1 = new Point3D(argLevels[1].EndBL_1.X, argLevels[1].EndBL_1.Y, .18);

                    argLevels[1].EndBL_2 = new Point3D(argLevels[1].EndBL_2.Value.X, argLevels[1].EndBL_2.Value.Y * .9, .3);
                }

                argLevels[3] = argLevels[1].CloneNegateZ();

                #endregion
                #region Far

                argLevels[0] = UtilityCore.Clone(arg);

                //argLevels[0].EndTL = new Point3D(argLevels[0].EndTL.X, argLevels[0].EndTL.Y * .7, .4);
                argLevels[0].EndTL = new Point3D(argLevels[0].EndTL.X, argLevels[0].EndTL.Y * .7, .5);

                //argLevels[0].EndTR = new Point3D(argLevels[0].EndTR.X * .5, argLevels[0].EndTR.Y * .6, .25);
                argLevels[0].EndTR = new Point3D(argLevels[0].EndTR.X * .5, argLevels[0].EndTR.Y * .6, -.25);

                if (argLevels[0].EndBL_2 == null)
                {
                    argLevels[0].EndBR = new Point3D(argLevels[0].EndBR.X * .5, argLevels[0].EndBR.Y * .6, .25);
                    argLevels[0].EndBL_1 = new Point3D(argLevels[0].EndBL_1.X, argLevels[0].EndBL_1.Y * .6, .4);
                }
                else
                {
                    // Bottom Right
                    Vector3D offset = (argLevels[1].EndBR - argLevels[1].EndBL_1) * .5;
                    Point3D startPoint = argLevels[1].EndBL_1 + offset;     // midway along bottom edge

                    offset = argLevels[1].EndTR - startPoint;

                    argLevels[0].EndBR = startPoint + (offset * .15);       // from midway point toward upper right point
                    argLevels[0].EndBR = new Point3D(argLevels[0].EndBR.X, argLevels[0].EndBR.Y, .25);      // fix z

                    // Left of bottom right (where the circle cutout ends)
                    offset = argLevels[1].EndBR - argLevels[1].EndBL_1;
                    argLevels[0].EndBL_1 = Math3D.GetClosestPoint_Line_Point(argLevels[0].EndBR, offset, argLevels[0].EndBL_1);

                    offset *= .05;
                    Point3D minBL1 = argLevels[0].EndBR - offset;
                    Vector3D testOffset = argLevels[0].EndBL_1 - argLevels[0].EndBR;
                    if (Vector3D.DotProduct(testOffset, offset) < 0 || testOffset.LengthSquared < offset.LengthSquared)
                    {
                        argLevels[0].EndBL_1 = minBL1;
                    }

                    // Bottom Left
                    argLevels[0].EndBL_2 = new Point3D(argLevels[0].EndBL_2.Value.X, argLevels[0].EndBL_2.Value.Y * .6, .4);

                    // Reduce the curve a bit
                    argLevels[0].B2AngleL = argLevels[0].B2AngleL * .9;
                    argLevels[0].B2PercentL = argLevels[0].B2PercentL * .95;

                    argLevels[0].B2AngleR = argLevels[0].B2AngleR * .85;
                    argLevels[0].B2PercentR = argLevels[0].B2PercentR * .95;
                }

                argLevels[4] = argLevels[0].CloneNegateZ();

                #endregion

                #region Get polygon points

                BezierSegmentDef[] segments = TestAxeSimple2_Segments(argLevels[0]);

                List<Point3D> pointsDupes = new List<Point3D>();

                foreach (BezierSegmentDef bezier in segments)
                {
                    pointsDupes.AddRange(Math3D.GetBezierSegment(5, bezier));
                }

                List<Point3D> points = new List<Point3D>();
                points.Add(pointsDupes[0]);
                for (int cntr = 1; cntr < pointsDupes.Count; cntr++)
                {
                    if (!Math3D.IsNearValue(pointsDupes[cntr], pointsDupes[cntr - 1]))
                    {
                        points.Add(pointsDupes[cntr]);
                    }
                }

                #endregion

                //ITriangle plane = AveragePlaneTests.GetAveragePlane(points.ToArray());
                ITriangle plane = Math2D.GetPlane_Average(points.ToArray());

                PrepFor3D();

                foreach (Point3D point in points)
                {
                    AddDot(point, _endPointC, .0125);

                    AddLine(point, Math3D.GetClosestPoint_Plane_Point(plane, point), _controlPointC, 2);
                }

                //-------------------------

                //List<Point3D> upPoints = new List<Point3D>();
                //upPoints.Add(Math3D.GetCenter(points));

                //upPoints.AddRange(AveragePlaneTests.GetAveragePlane_UpVectors(points.ToArray()).Select(o => upPoints[0] + o/*.ToUnit()*/));

                //AddLines(Enumerable.Range(1, upPoints.Count - 1).Select(o => Tuple.Create(0, o)), upPoints.ToArray(), _otherLineC);

                //-------------------------

                //var ups = AveragePlaneTests.TestAveragePlane_UpVectors(points.ToArray());
                //Point3D center = Math3D.GetCenter(points);

                //AddLine(center, center + ups.Item1, _controlLineC, 2);
                //AddLine(center, center + ups.Item2, _controlLineC, 2);

                //-------------------------

                AddPlane(plane, UtilityWPF.ColorFromHex("20A0A0A0"), 25);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnAvgPlane2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Curves_AxeSimple2 arg = new Curves_AxeSimple2(false);
                Curves_AxeSimple2[] argLevels = new Curves_AxeSimple2[5];

                argLevels[2] = arg;     // Edge
                #region Middle

                argLevels[1] = UtilityCore.Clone(arg);

                // Right
                argLevels[1].EndTR = new Point3D(argLevels[1].EndTR.X * .8, argLevels[1].EndTR.Y * .9, .15);

                Vector3D offsetTR = new Point3D(argLevels[1].EndTR.X, argLevels[1].EndTR.Y, 0) - arg.EndTR;
                Quaternion angleTR = Math3D.GetRotation((arg.EndTL - arg.EndTR), offsetTR);

                Vector3D rotated = (arg.EndBL_1 - arg.EndBR).ToUnit() * offsetTR.Length;
                rotated = rotated.GetRotatedVector(angleTR.Axis, angleTR.Angle * -1.3);

                argLevels[1].EndBR = new Point3D(argLevels[1].EndBR.X + rotated.X, argLevels[1].EndBR.Y + rotated.Y, .15);      // can't just use percents of coordinates.  Instead use the same offset angle,distance that top left had

                // Left
                argLevels[1].EndTL = new Point3D(argLevels[1].EndTL.X, argLevels[1].EndTL.Y * .95, .3);

                if (argLevels[1].EndBL_2 == null)
                {
                    argLevels[1].EndBL_1 = new Point3D(argLevels[1].EndBL_1.X, argLevels[1].EndBL_1.Y * .9, .3);
                }
                else
                {
                    Vector3D offsetBL1 = arg.EndBR - arg.EndBL_1;
                    double lengthBL1 = (new Point3D(argLevels[1].EndBR.X, argLevels[1].EndBR.Y, 0) - arg.EndBR).Length;
                    offsetBL1 = offsetBL1.ToUnit() * (offsetBL1.Length - lengthBL1);

                    argLevels[1].EndBL_1 = argLevels[1].EndBR - offsetBL1;
                    argLevels[1].EndBL_1 = new Point3D(argLevels[1].EndBL_1.X, argLevels[1].EndBL_1.Y, .18);

                    argLevels[1].EndBL_2 = new Point3D(argLevels[1].EndBL_2.Value.X, argLevels[1].EndBL_2.Value.Y * .9, .3);
                }

                argLevels[3] = argLevels[1].CloneNegateZ();

                #endregion
                #region Far

                argLevels[0] = UtilityCore.Clone(arg);

                //argLevels[0].EndTL = new Point3D(argLevels[0].EndTL.X, argLevels[0].EndTL.Y * .7, .4);
                argLevels[0].EndTL = new Point3D(argLevels[0].EndTL.X, argLevels[0].EndTL.Y * .7, .5);

                //argLevels[0].EndTR = new Point3D(argLevels[0].EndTR.X * .5, argLevels[0].EndTR.Y * .6, .25);
                argLevels[0].EndTR = new Point3D(argLevels[0].EndTR.X * .5, argLevels[0].EndTR.Y * .6, -.25);

                if (argLevels[0].EndBL_2 == null)
                {
                    argLevels[0].EndBR = new Point3D(argLevels[0].EndBR.X * .5, argLevels[0].EndBR.Y * .6, .25);
                    argLevels[0].EndBL_1 = new Point3D(argLevels[0].EndBL_1.X, argLevels[0].EndBL_1.Y * .6, .4);
                }
                else
                {
                    // Bottom Right
                    Vector3D offset = (argLevels[1].EndBR - argLevels[1].EndBL_1) * .5;
                    Point3D startPoint = argLevels[1].EndBL_1 + offset;     // midway along bottom edge

                    offset = argLevels[1].EndTR - startPoint;

                    argLevels[0].EndBR = startPoint + (offset * .15);       // from midway point toward upper right point
                    argLevels[0].EndBR = new Point3D(argLevels[0].EndBR.X, argLevels[0].EndBR.Y, .25);      // fix z

                    // Left of bottom right (where the circle cutout ends)
                    offset = argLevels[1].EndBR - argLevels[1].EndBL_1;
                    argLevels[0].EndBL_1 = Math3D.GetClosestPoint_Line_Point(argLevels[0].EndBR, offset, argLevels[0].EndBL_1);

                    offset *= .05;
                    Point3D minBL1 = argLevels[0].EndBR - offset;
                    Vector3D testOffset = argLevels[0].EndBL_1 - argLevels[0].EndBR;
                    if (Vector3D.DotProduct(testOffset, offset) < 0 || testOffset.LengthSquared < offset.LengthSquared)
                    {
                        argLevels[0].EndBL_1 = minBL1;
                    }

                    // Bottom Left
                    argLevels[0].EndBL_2 = new Point3D(argLevels[0].EndBL_2.Value.X, argLevels[0].EndBL_2.Value.Y * .6, .4);

                    // Reduce the curve a bit
                    argLevels[0].B2AngleL = argLevels[0].B2AngleL * .9;
                    argLevels[0].B2PercentL = argLevels[0].B2PercentL * .95;

                    argLevels[0].B2AngleR = argLevels[0].B2AngleR * .85;
                    argLevels[0].B2PercentR = argLevels[0].B2PercentR * .95;
                }

                argLevels[4] = argLevels[0].CloneNegateZ();

                #endregion

                #region Get polygon points

                BezierSegmentDef[] segments = TestAxeSimple2_Segments(argLevels[0]);

                List<Point3D> pointsDupes = new List<Point3D>();

                foreach (BezierSegmentDef bezier in segments)
                {
                    pointsDupes.AddRange(Math3D.GetBezierSegment(5, bezier));
                }

                List<Point3D> points = new List<Point3D>();
                points.Add(pointsDupes[0]);
                for (int cntr = 1; cntr < pointsDupes.Count; cntr++)
                {
                    if (!Math3D.IsNearValue(pointsDupes[cntr], pointsDupes[cntr - 1]))
                    {
                        points.Add(pointsDupes[cntr]);
                    }
                }

                #endregion

                TriangleIndexed[] triangles = Math2D.GetTrianglesFromConcavePoly3D(points.ToArray());

                PrepFor3D();

                foreach (Point3D point in points)
                {
                    AddDot(point, _endPointC, .0125);
                }

                foreach (ITriangle triangle in triangles)
                {
                    DrawTrianglePlate(triangle.Point0, triangle.Point1, triangle.Point2, UtilityWPF.GetRandomColor(128, 64, 192), false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void Test2Segments2D(Point end1, Point end2, Point end3)
        {
            // Control points for 2
            var controlPoints = BezierSegmentDef.GetControlPoints_Middle(end1, end2, end3, trkSingleLine2Segments2DPercent.Value, trkSingleLine2Segments2DPercent.Value);

            Point3D[] allEndPoints = new[] { end1, end2, end3 }.Select(o => o.ToPoint3D()).ToArray();

            var segment12 = new BezierSegmentDef(0, 1, new[] { end1.ToPoint3D(), controlPoints.Item1.ToPoint3D() }, allEndPoints);
            var segment23 = new BezierSegmentDef(1, 2, new[] { controlPoints.Item2.ToPoint3D(), end3.ToPoint3D() }, allEndPoints);

            // Calculate the beziers
            Point3D[] bezierPoints12 = Math3D.GetBezierSegment(100, segment12);
            Point3D[] bezierPoints23 = Math3D.GetBezierSegment(100, segment23);

            #region Draw

            PrepFor2D();

            foreach (var bezier in new[] { Tuple.Create(segment12, bezierPoints12), Tuple.Create(segment23, bezierPoints23) })
            {
                // Main Line
                for (int cntr = 0; cntr < bezier.Item2.Length - 1; cntr++)
                {
                    AddLine(bezier.Item2[cntr].ToPoint2D(), bezier.Item2[cntr + 1].ToPoint2D(), _mainLineB, 3);
                }

                if (chkShowDots.IsChecked.Value)
                {
                    foreach (Point3D point in bezier.Item2)
                    {
                        AddDot(point.ToPoint2D(), _mainLineB, 8);
                    }

                    // Control Lines
                    Point[] allPoints = UtilityCore.Iterate<Point3D>(bezier.Item1.EndPoint0, bezier.Item1.ControlPoints, bezier.Item1.EndPoint1).Select(o => o.ToPoint2D()).ToArray();

                    for (int cntr = 0; cntr < allPoints.Length - 1; cntr++)
                    {
                        AddLine(allPoints[cntr], allPoints[cntr + 1], _controlLineB, 1);
                    }

                    for (int cntr = 0; cntr < bezier.Item1.ControlPoints.Length; cntr++)
                    {
                        AddDot(bezier.Item1.ControlPoints[cntr].ToPoint2D(), _controlPointB);
                    }
                }
            }

            // End Points
            if (chkShowDots.IsChecked.Value)
            {
                AddDot(end1, _endPointB);
                AddDot(end2, _endPointB);
                AddDot(end3, _endPointB);
            }

            #endregion
        }
        private void TestMultiSegment3D(Point3D[] ends)
        {
            BezierSegmentDef[] segments = BezierSegmentDef.GetBezierSegments(ends, trkSingleLineMultiSegments3DPercent.Value, chkIsClosed3D.IsChecked.Value ? true : (bool?)null);

            Point3D[] bezierPoints = Math3D.GetBezierPath(200, segments);

            PrepFor3D();

            // Main Line
            for (int cntr = 0; cntr < bezierPoints.Length - 1; cntr++)
            {
                AddLine(bezierPoints[cntr], bezierPoints[cntr + 1], _mainLineC, 2);
            }

            if (chkIsClosed3D.IsChecked.Value)
            {
                AddLine(bezierPoints[bezierPoints.Length - 1], bezierPoints[0], _mainLineC, 2);
            }

            if (chkShowDots.IsChecked.Value)
            {
                foreach (Point3D point in bezierPoints)
                {
                    AddDot(point, _mainLineC, .05);
                }

                foreach (BezierSegmentDef segment in segments)
                {
                    // Control Lines
                    for (int cntr = 0; cntr < segment.Combined.Length - 1; cntr++)
                    {
                        AddLine(segment.Combined[cntr], segment.Combined[cntr + 1], _controlLineC);
                    }

                    for (int cntr = 0; cntr < segment.ControlPoints.Length; cntr++)
                    {
                        AddDot(segment.ControlPoints[cntr], _controlPointC);
                    }

                    // End Points
                    foreach (Point3D end in ends)
                    {
                        AddDot(end, _endPointC);
                    }
                }
            }
        }

        private void TestAxeSimple1(AxeSimple1 arg)
        {
            BezierSegmentDef[][] segmentSets = new BezierSegmentDef[5][];

            PrepFor3D();

            #region Lines

            for (int cntr = 0; cntr < 5; cntr++)
            {
                if (!chkAxe3D.IsChecked.Value && cntr != 2)
                {
                    continue;
                }

                #region scale, z

                double zL, zR, scaleX, scaleY;

                switch (cntr)
                {
                    case 0:
                    case 4:
                        zL = arg.Z2L;
                        zR = arg.Z2R;
                        scaleX = arg.Scale2X;
                        scaleY = arg.Scale2Y;
                        break;

                    case 1:
                    case 3:
                        zL = zR = arg.Z1;
                        scaleX = arg.Scale1X;
                        scaleY = arg.Scale1Y;
                        break;

                    case 2:
                        zL = zR = 0;
                        scaleX = scaleY = 1;
                        break;

                    default:
                        throw new ApplicationException("Unknown cntr: " + cntr.ToString());
                }

                if (cntr < 2)
                {
                    zL *= -1d;
                    zR *= -1d;
                }

                #endregion

                Point3D[] endPoints = new[]
                    {
                        new Point3D(-arg.leftX, -arg.leftY * scaleY, zL),     // top left
                        new Point3D(-arg.leftX, arg.leftY * scaleY, zL),     // bottom left
                        new Point3D(arg.rightX * scaleX, -arg.rightY * scaleY, zR),       // top right
                        new Point3D(arg.rightX * scaleX, arg.rightY * scaleY, zR),        // bottom right
                    };

                BezierSegmentDef[] segments = TestAxeSimple1_Segments(endPoints, arg);
                segmentSets[cntr] = segments;

                DrawBezierLines(segments, chkAxeLine.IsChecked.Value, chkAxeControl.IsChecked.Value, chkAxeEnd.IsChecked.Value);
            }

            #endregion
            #region Plates

            if (chkAxe3D.IsChecked.Value)
            {
                Color edgeColor = Colors.GhostWhite;
                Color middleColor = Colors.DimGray;

                int squareCount = 8;

                DrawBezierPlates(squareCount, segmentSets[0], segmentSets[1], middleColor, false);

                DrawBezierPlates(squareCount, new[] { segmentSets[1][0], segmentSets[1][1] }, new[] { segmentSets[2][0], segmentSets[2][1] }, middleColor, false);
                DrawBezierPlate(squareCount, segmentSets[1][2], segmentSets[2][2], edgeColor, true);

                DrawBezierPlates(squareCount, new[] { segmentSets[2][0], segmentSets[2][1] }, new[] { segmentSets[3][0], segmentSets[3][1] }, middleColor, false);
                DrawBezierPlate(squareCount, segmentSets[2][2], segmentSets[3][2], edgeColor, true);

                DrawBezierPlates(squareCount, segmentSets[3], segmentSets[4], middleColor, false);

                // End cap plates
                if (arg.isCenterFilled)
                {
                    for (int cntr = 0; cntr < 2; cntr++)
                    {
                        int index = cntr == 0 ? 0 : 4;

                        DrawBezierPlate(squareCount, segmentSets[index][0], segmentSets[index][1], middleColor, false);        // top - bottom

                        BezierSegmentDef extraSeg = new BezierSegmentDef(segmentSets[index][2].EndIndex0, segmentSets[index][2].EndIndex1, null, segmentSets[index][2].AllEndPoints);
                        DrawBezierPlate(squareCount, extraSeg, segmentSets[index][2], middleColor, false);     // edge
                    }
                }
                else
                {
                    DrawBezierPlates(squareCount, segmentSets[0], segmentSets[4], middleColor, false);
                }
            }

            #endregion
        }
        private static BezierSegmentDef[] TestAxeSimple1_Segments(Point3D[] endPoints, AxeSimple1 arg)
        {
            const int TOPLEFT = 0;
            const int BOTTOMLEFT = 1;
            const int TOPRIGHT = 2;
            const int BOTTOMRIGHT = 3;

            // Edge
            Point3D controlTR = BezierSegmentDef.GetControlPoint_End(endPoints[TOPRIGHT], endPoints[BOTTOMRIGHT], endPoints[TOPLEFT], true, arg.edgeAngle, arg.edgePercent);
            Point3D controlBR = BezierSegmentDef.GetControlPoint_End(endPoints[BOTTOMRIGHT], endPoints[TOPRIGHT], endPoints[BOTTOMLEFT], true, arg.edgeAngle, arg.edgePercent);
            BezierSegmentDef edge = new BezierSegmentDef(TOPRIGHT, BOTTOMRIGHT, new[] { controlTR, controlBR }, endPoints);

            // Bottom
            Point3D controlBL = BezierSegmentDef.GetControlPoint_End(endPoints[BOTTOMLEFT], endPoints[BOTTOMRIGHT], endPoints[TOPRIGHT], arg.leftAway, arg.leftAngle, arg.leftPercent);
            controlBR = BezierSegmentDef.GetControlPoint_End(endPoints[BOTTOMRIGHT], endPoints[BOTTOMLEFT], endPoints[TOPRIGHT], arg.rightAway, arg.rightAngle, arg.rightPercent);
            BezierSegmentDef bottom = new BezierSegmentDef(BOTTOMLEFT, BOTTOMRIGHT, new[] { controlBL, controlBR }, endPoints);

            // Top
            Point3D controlTL = BezierSegmentDef.GetControlPoint_End(endPoints[TOPLEFT], endPoints[TOPRIGHT], endPoints[BOTTOMRIGHT], arg.leftAway, arg.leftAngle, arg.leftPercent);
            controlTR = BezierSegmentDef.GetControlPoint_End(endPoints[TOPRIGHT], endPoints[TOPLEFT], endPoints[BOTTOMRIGHT], arg.rightAway, arg.rightAngle, arg.rightPercent);
            BezierSegmentDef top = new BezierSegmentDef(TOPLEFT, TOPRIGHT, new[] { controlTL, controlTR }, endPoints);

            return new[] { bottom, top, edge };
        }

        private void TestAxeSimple2(Curves_AxeSimple2 arg)
        {
            BezierSegmentDef[][] segmentSets = new BezierSegmentDef[5][];

            Curves_AxeSimple2[] argLevels = new Curves_AxeSimple2[5];

            argLevels[2] = arg;     // Edge
            #region Middle

            argLevels[1] = UtilityCore.Clone(arg);

            // Right
            argLevels[1].EndTR = new Point3D(argLevels[1].EndTR.X * .8, argLevels[1].EndTR.Y * .9, .15);

            Vector3D offsetTR = new Point3D(argLevels[1].EndTR.X, argLevels[1].EndTR.Y, 0) - arg.EndTR;
            Quaternion angleTR = Math3D.GetRotation((arg.EndTL - arg.EndTR), offsetTR);

            Vector3D rotated = (arg.EndBL_1 - arg.EndBR).ToUnit() * offsetTR.Length;
            rotated = rotated.GetRotatedVector(angleTR.Axis, angleTR.Angle * -1.3);

            argLevels[1].EndBR = new Point3D(argLevels[1].EndBR.X + rotated.X, argLevels[1].EndBR.Y + rotated.Y, .15);      // can't just use percents of coordinates.  Instead use the same offset angle,distance that top left had

            // Left
            argLevels[1].EndTL = new Point3D(argLevels[1].EndTL.X, argLevels[1].EndTL.Y * .95, .3);

            if (argLevels[1].EndBL_2 == null)
            {
                argLevels[1].EndBL_1 = new Point3D(argLevels[1].EndBL_1.X, argLevels[1].EndBL_1.Y * .9, .3);
            }
            else
            {
                Vector3D offsetBL1 = arg.EndBR - arg.EndBL_1;
                double lengthBL1 = (new Point3D(argLevels[1].EndBR.X, argLevels[1].EndBR.Y, 0) - arg.EndBR).Length;
                offsetBL1 = offsetBL1.ToUnit() * (offsetBL1.Length - lengthBL1);

                argLevels[1].EndBL_1 = argLevels[1].EndBR - offsetBL1;
                argLevels[1].EndBL_1 = new Point3D(argLevels[1].EndBL_1.X, argLevels[1].EndBL_1.Y, .18);

                argLevels[1].EndBL_2 = new Point3D(argLevels[1].EndBL_2.Value.X, argLevels[1].EndBL_2.Value.Y * .9, .3);
            }

            argLevels[3] = argLevels[1].CloneNegateZ();

            #endregion
            #region Far

            argLevels[0] = UtilityCore.Clone(arg);

            argLevels[0].EndTL = new Point3D(argLevels[0].EndTL.X, argLevels[0].EndTL.Y * .7, .4);

            argLevels[0].EndTR = new Point3D(argLevels[0].EndTR.X * .5, argLevels[0].EndTR.Y * .6, .25);

            if (argLevels[0].EndBL_2 == null)
            {
                argLevels[0].EndBR = new Point3D(argLevels[0].EndBR.X * .5, argLevels[0].EndBR.Y * .6, .25);
                argLevels[0].EndBL_1 = new Point3D(argLevels[0].EndBL_1.X, argLevels[0].EndBL_1.Y * .6, .4);
            }
            else
            {
                // Bottom Right
                Vector3D offset = (argLevels[1].EndBR - argLevels[1].EndBL_1) * .5;
                Point3D startPoint = argLevels[1].EndBL_1 + offset;     // midway along bottom edge

                offset = argLevels[1].EndTR - startPoint;

                argLevels[0].EndBR = startPoint + (offset * .15);       // from midway point toward upper right point
                argLevels[0].EndBR = new Point3D(argLevels[0].EndBR.X, argLevels[0].EndBR.Y, .25);      // fix z

                // Left of bottom right (where the circle cutout ends)
                offset = argLevels[1].EndBR - argLevels[1].EndBL_1;
                argLevels[0].EndBL_1 = Math3D.GetClosestPoint_Line_Point(argLevels[0].EndBR, offset, argLevels[0].EndBL_1);

                offset *= .05;
                Point3D minBL1 = argLevels[0].EndBR - offset;
                Vector3D testOffset = argLevels[0].EndBL_1 - argLevels[0].EndBR;
                if (Vector3D.DotProduct(testOffset, offset) < 0 || testOffset.LengthSquared < offset.LengthSquared)
                {
                    argLevels[0].EndBL_1 = minBL1;
                }

                // Bottom Left
                argLevels[0].EndBL_2 = new Point3D(argLevels[0].EndBL_2.Value.X, argLevels[0].EndBL_2.Value.Y * .6, .4);

                // Reduce the curve a bit
                argLevels[0].B2AngleL = argLevels[0].B2AngleL * .9;
                argLevels[0].B2PercentL = argLevels[0].B2PercentL * .95;

                argLevels[0].B2AngleR = argLevels[0].B2AngleR * .85;
                argLevels[0].B2PercentR = argLevels[0].B2PercentR * .95;
            }

            argLevels[4] = argLevels[0].CloneNegateZ();

            #endregion

            PrepFor3D();

            #region Lines

            for (int cntr = 0; cntr < 5; cntr++)
            {
                if (!chkAxe3D.IsChecked.Value && cntr != 2)
                {
                    continue;
                }

                Curves_AxeSimple2 argCurrent = argLevels[cntr];

                BezierSegmentDef[] segments = TestAxeSimple2_Segments(argCurrent);
                segmentSets[cntr] = segments;

                DrawBezierLines(segments, chkAxeLine.IsChecked.Value, chkAxeControl.IsChecked.Value, chkAxeEnd.IsChecked.Value);
            }

            #endregion
            #region Plates

            if (chkAxe3D.IsChecked.Value)
            {
                Color edgeColor = Colors.GhostWhite;
                Color middleColor = Colors.DimGray;

                int numSegments = segmentSets[0].Length;

                int squareCount = 20;

                // Z to Z
                DrawBezierPlates(squareCount, segmentSets[0], segmentSets[1], middleColor, false);

                if (numSegments == 3)
                {
                    DrawBezierPlates(squareCount, new[] { segmentSets[1][0], segmentSets[1][2] }, new[] { segmentSets[2][0], segmentSets[2][2] }, middleColor, false);
                    DrawBezierPlate(squareCount, segmentSets[1][1], segmentSets[2][1], edgeColor, true);

                    DrawBezierPlates(squareCount, new[] { segmentSets[2][0], segmentSets[2][2] }, new[] { segmentSets[3][0], segmentSets[3][2] }, middleColor, false);
                    DrawBezierPlate(squareCount, segmentSets[2][1], segmentSets[3][1], edgeColor, true);
                }
                else
                {
                    DrawBezierPlates(squareCount, new[] { segmentSets[1][0], segmentSets[1][3] }, new[] { segmentSets[2][0], segmentSets[2][3] }, middleColor, false);
                    DrawBezierPlates(squareCount, new[] { segmentSets[1][1], segmentSets[1][2] }, new[] { segmentSets[2][1], segmentSets[2][2] }, edgeColor, true);

                    DrawBezierPlates(squareCount, new[] { segmentSets[2][0], segmentSets[2][3] }, new[] { segmentSets[3][0], segmentSets[3][3] }, middleColor, false);
                    DrawBezierPlates(squareCount, new[] { segmentSets[2][1], segmentSets[2][2] }, new[] { segmentSets[3][1], segmentSets[3][2] }, edgeColor, true);
                }

                DrawBezierPlates(squareCount, segmentSets[3], segmentSets[4], middleColor, false);

                // End cap plates
                for (int cntr = 0; cntr < 2; cntr++)
                {
                    int index = cntr == 0 ? 0 : 4;

                    // Turn the end cap into a polygon, then triangulate it
                    Point3D[] endCapPoly = Math3D.GetBezierPath(squareCount, segmentSets[index].Select(o => new[] { o }).ToArray());       // Call the jagged array overload so that the individual bezier end points don't get smoothed out

                    TriangleIndexed[] endCapTriangles = Math2D.GetTrianglesFromConcavePoly3D(endCapPoly);
                    if (cntr == 0)
                    {
                        endCapTriangles = endCapTriangles.Select(o => new TriangleIndexed(o.Index0, o.Index2, o.Index1, o.AllPoints)).ToArray();        // need to do this so the normals point in the proper direction
                    }

                    DrawPolyPlate(endCapTriangles, middleColor, false);

                    //AddLines(endCapTriangles.Select(o => Tuple.Create(o.GetCenterPoint(), o.GetCenterPoint() + o.Normal)), _controlLineC, 2);
                }
            }

            #endregion
        }
        private static BezierSegmentDef[] TestAxeSimple2_Segments(Curves_AxeSimple2 arg)
        {
            Point3D[] points = arg.GetAllPoints();

            // Top
            BezierSegmentDef top = new BezierSegmentDef(arg.IndexTL, arg.IndexTR, null, points);

            // Edge
            Point3D controlTR = BezierSegmentDef.GetControlPoint_End(arg.EndTR, arg.EndBR, arg.EndBL_1, true, arg.EdgeAngleT, arg.EdgePercentT);
            Point3D controlBR = BezierSegmentDef.GetControlPoint_End(arg.EndBR, arg.EndTR, arg.EndTL, true, arg.EdgeAngleB, arg.EdgePercentB);
            BezierSegmentDef edge = new BezierSegmentDef(arg.IndexTR, arg.IndexBR, new[] { controlTR, controlBR }, points);

            // Bottom (right portion)
            BezierSegmentDef bottomRight = null;
            if (arg.EndBL_2 == null)
            {
                Point3D controlR = BezierSegmentDef.GetControlPoint_End(arg.EndBR, arg.EndBL_1, arg.EndTR, false, arg.B1AngleR, arg.B1PercentR);
                Point3D controlL = BezierSegmentDef.GetControlPoint_End(arg.EndBL_1, arg.EndBR, arg.EndTR, false, arg.B1AngleL, arg.B1PercentL);
                bottomRight = new BezierSegmentDef(arg.IndexBR, arg.IndexBL_1, new[] { controlR, controlL }, points);
            }
            else
            {
                bottomRight = new BezierSegmentDef(arg.IndexBR, arg.IndexBL_1, null, points);
            }

            // Bottom (left portion)
            BezierSegmentDef bottomLeft = null;
            if (arg.EndBL_2 != null)
            {
                Point3D controlR = BezierSegmentDef.GetControlPoint_End(arg.EndBL_1, arg.EndBL_2.Value, arg.EndTR, false, arg.B2AngleR, arg.B2PercentR);
                Point3D controlL = BezierSegmentDef.GetControlPoint_End(arg.EndBL_2.Value, arg.EndBL_1, arg.EndTR, false, arg.B2AngleL, arg.B2PercentL);
                bottomLeft = new BezierSegmentDef(arg.IndexBL_1, arg.IndexBL_2, new[] { controlR, controlL }, points);
            }

            return UtilityCore.Iterate<BezierSegmentDef>(top, edge, bottomRight, bottomLeft).ToArray();
        }

        private void PrepFor2D()
        {
            _canvas.Children.Clear();

            _viewport.Visibility = Visibility.Collapsed;
            _canvas.Visibility = Visibility.Visible;
        }
        private void PrepFor3D()
        {
            _viewport.Children.RemoveAll(_visuals);

            _canvas.Visibility = Visibility.Hidden;     //NOTE: If this is set to collapsed, then the first time it's made visible, its width and height will be zero (until the window gets a chance to recalc)
            _viewport.Visibility = Visibility.Visible;
        }

        private void DrawBezierLines(BezierSegmentDef[] beziers, bool showLines, bool showControls, bool showEnds)
        {
            // Main Line
            if (showLines)
            {
                foreach (BezierSegmentDef bezier in beziers)
                {
                    AddLines(Math3D.GetBezierSegment(100, bezier), _mainLineC, 2);
                }
            }

            // Control Lines
            if (showControls)
            {
                foreach (BezierSegmentDef bezier in beziers)
                {
                    if (bezier.ControlPoints == null)
                    {
                        continue;
                    }

                    AddLines(bezier.Combined, _controlLineC);

                    for (int cntr = 0; cntr < bezier.ControlPoints.Length; cntr++)
                    {
                        AddDot(bezier.ControlPoints[cntr], _controlPointC);
                    }
                }
            }

            // End Points
            if (showEnds)
            {
                foreach (Point3D points in beziers[0].AllEndPoints)
                {
                    AddDot(points, _endPointC);
                }
            }
        }

        private void DrawBezierPlates(int count, BezierSegmentDef[] seg1, BezierSegmentDef[] seg2, Color color, bool isShiny, bool ensureNormalsPointTheSame = false)
        {
            for (int cntr = 0; cntr < seg1.Length; cntr++)
            {
                DrawBezierPlate(count, seg1[cntr], seg2[cntr], color, isShiny, ensureNormalsPointTheSame);
            }
        }
        private void DrawBezierPlate(int count, BezierSegmentDef seg1, BezierSegmentDef seg2, Color color, bool isShiny, bool ensureNormalsPointTheSame = false)
        {
            Point3D[] rim1 = Math3D.GetBezierSegment(count, seg1);
            Point3D[] rim2 = Math3D.GetBezierSegment(count, seg2);

            Point3D[] allPoints = UtilityCore.Iterate(rim1, rim2).ToArray();

            List<TriangleIndexed> triangles = new List<TriangleIndexed>();

            for (int cntr = 0; cntr < count - 1; cntr++)
            {
                triangles.Add(new TriangleIndexed(count + cntr, count + cntr + 1, cntr, allPoints));        // bottom left
                triangles.Add(new TriangleIndexed(cntr + 1, cntr, count + cntr + 1, allPoints));        // top right
            }

            #region Ensure all normals point the same way

            //Doesn't work
            //if (ensureNormalsPointTheSame)
            //{
            //    Vector3D firstNormal = triangles[0].Normal;
            //    bool[] sameNormals = triangles.Select(o => Vector3D.DotProduct(firstNormal, o.Normal) > 0).ToArray();

            //    int sameCount = sameNormals.Where(o => o).Count();

            //    if (sameCount != triangles.Count)
            //    {
            //        // Some up, some down.  Majority rules
            //        bool fixDifferents = sameCount > triangles.Count / 2;

            //        for(int cntr = 0; cntr < triangles.Count; cntr++)
            //        {
            //            if(sameNormals[cntr] != fixDifferents)
            //            {
            //                triangles[cntr] = new TriangleIndexed(triangles[cntr].Index1, triangles[cntr].Index0, triangles[cntr].Index2, triangles[cntr].AllPoints);
            //            }
            //        }
            //    }
            //}

            #endregion

            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            if (isShiny)
            {
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(color, Colors.White, .5d)), 5d));
            }
            else
            {
                Color derivedColor = UtilityWPF.AlphaBlend(color, Colors.White, .8d);
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(128, derivedColor.R, derivedColor.G, derivedColor.B)), 2d));
            }

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = UtilityWPF.GetMeshFromTriangles(triangles.ToArray());

            // Model Visual
            ModelVisual3D model = new ModelVisual3D();
            model.Content = geometry;

            // Temporarily add to the viewport
            _viewport.Children.Add(model);
            _visuals.Add(model);
        }
        private void DrawTrianglePlate(Point3D point0, Point3D point1, Point3D point2, Color color, bool isShiny)
        {
            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            if (isShiny)
            {
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(color, Colors.White, .5d)), 5d));
            }
            else
            {
                Color derivedColor = UtilityWPF.AlphaBlend(color, Colors.White, .8d);
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(128, derivedColor.R, derivedColor.G, derivedColor.B)), 2d));
            }

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = UtilityWPF.GetMeshFromTriangles(new[] { new TriangleIndexed(0, 1, 2, new[] { point0, point1, point2 }) });

            // Model Visual
            ModelVisual3D model = new ModelVisual3D();
            model.Content = geometry;

            // Temporarily add to the viewport
            _viewport.Children.Add(model);
            _visuals.Add(model);
        }
        private void DrawPolyPlate(ITriangleIndexed[] triangles, Color color, bool isShiny)
        {
            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            if (isShiny)
            {
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(color, Colors.White, .5d)), 5d));
            }
            else
            {
                Color derivedColor = UtilityWPF.AlphaBlend(color, Colors.White, .8d);
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(128, derivedColor.R, derivedColor.G, derivedColor.B)), 2d));
            }

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = UtilityWPF.GetMeshFromTriangles(triangles);

            // Model Visual
            ModelVisual3D model = new ModelVisual3D();
            model.Content = geometry;

            // Temporarily add to the viewport
            _viewport.Children.Add(model);
            _visuals.Add(model);
        }

        private void AddDot(Point position, Brush brush, double size = 16)
        {
            Ellipse dot = new Ellipse()
            {
                Fill = brush,
                Width = size,
                Height = size
            };

            double halfSize = size / 2d;

            Canvas.SetLeft(dot, position.X - halfSize);
            Canvas.SetTop(dot, position.Y - halfSize);

            _canvas.Children.Add(dot);
        }
        private void AddLine(Point from, Point to, Brush brush, double width = 2)
        {
            Line line = new Line()
            {
                X1 = from.X,
                Y1 = from.Y,
                X2 = to.X,
                Y2 = to.Y,
                Stroke = brush,
                StrokeThickness = width
            };

            _canvas.Children.Add(line);
        }
        private void AddBezier(Point from, Point fromControl, Point to, Point toControl, Brush brush, double width = 2)
        {
            PathFigure figure = new PathFigure() { IsClosed = false };
            figure.StartPoint = from;
            figure.Segments.Add(new BezierSegment() { Point1 = fromControl, Point2 = toControl, Point3 = to });

            PathGeometry geometry = new PathGeometry();
            geometry.Figures.Add(figure);

            Path path = new Path();
            path.Stroke = brush;
            path.StrokeThickness = width;
            path.Data = geometry;

            _canvas.Children.Add(path);
        }

        private void AddDot(Point3D position, Color color, double radius = .1)
        {
            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(color, Colors.White, .5d)), 50d));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = UtilityWPF.GetSphere_LatLon(3, radius, radius, radius);

            // Model Visual
            ModelVisual3D model = new ModelVisual3D();
            model.Content = geometry;
            model.Transform = new TranslateTransform3D(position.ToVector());

            // Temporarily add to the viewport
            _viewport.Children.Add(model);
            _visuals.Add(model);
        }
        private void AddLine(Point3D from, Point3D to, Color color, double thickness = 1d)
        {
            ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
            lineVisual.Thickness = thickness;
            lineVisual.Color = color;
            lineVisual.AddLine(from, to);

            _viewport.Children.Add(lineVisual);
            _visuals.Add(lineVisual);
        }
        private void AddLines(IEnumerable<Tuple<int, int>> lines, Point3D[] points, Color color, double thickness = 1d)
        {
            // Draw the lines
            ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
            lineVisual.Thickness = thickness;
            lineVisual.Color = color;

            foreach (var line in lines)
            {
                lineVisual.AddLine(points[line.Item1], points[line.Item2]);
            }

            _viewport.Children.Add(lineVisual);
            _visuals.Add(lineVisual);
        }
        private void AddLines(IEnumerable<Tuple<Point3D, Point3D>> lines, Color color, double thickness = 1d)
        {
            // Draw the lines
            ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
            lineVisual.Thickness = thickness;
            lineVisual.Color = color;

            foreach (var line in lines)
            {
                lineVisual.AddLine(line.Item1, line.Item2);
            }

            _viewport.Children.Add(lineVisual);
            _visuals.Add(lineVisual);
        }
        private void AddLines(Point3D[] points, Color color, double thickness = 1d)
        {
            ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
            lineVisual.Thickness = thickness;
            lineVisual.Color = color;

            for (int cntr = 0; cntr < points.Length - 1; cntr++)
            {
                lineVisual.AddLine(points[cntr], points[cntr + 1]);
            }

            _viewport.Children.Add(lineVisual);
            _visuals.Add(lineVisual);
        }
        private void AddPlane(ITriangle plane, Color color, double size = 20)
        {
            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            //materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(color, Colors.White, .5d)), 5d));

            // Build a differently sized triangle
            ITriangle plane2 = new Triangle(
                plane.Point1 + ((plane.Point0 - plane.Point1) * size),
                plane.Point2 + ((plane.Point1 - plane.Point2) * size),
                plane.Point0 + ((plane.Point2 - plane.Point0) * size)
                );

            // Create the points for the plane
            Point3D[] points = new Point3D[4];
            points[0] = plane2.Point0;
            points[1] = plane2.Point1;
            points[2] = plane2.Point2;
            points[3] = Math3D.FromBarycentric(plane2, new Vector(1, 1));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = UtilityWPF.GetMeshFromTriangles(new[] { new TriangleIndexed(0, 1, 2, points), new TriangleIndexed(1, 3, 2, points) });

            // Model Visual
            ModelVisual3D model = new ModelVisual3D();
            model.Content = geometry;

            // Temporarily add to the viewport
            _viewport.Children.Add(model);
            _visuals.Add(model);
        }

        #endregion
    }

    #region Class: AxeSimple2

    public class Curves_AxeSimple2     // UtilityCore.Clone can't work with nested classes
    {
        public Curves_AxeSimple2() { }      // default constructor for clone
        public Curves_AxeSimple2(bool isFixed)
        {
            if (isFixed)
            {
                Define_Fixed();
            }
            else
            {
                Define_Variable();
            }
        }

        // Points
        public Point3D EndTL { get; set; }
        public Point3D EndTR { get; set; }
        public Point3D EndBR { get; set; }
        public Point3D EndBL_1 { get; set; }
        public Point3D? EndBL_2 { get; set; }

        public readonly int IndexTL = 0;
        public readonly int IndexTR = 1;
        public readonly int IndexBR = 2;
        public readonly int IndexBL_1 = 3;
        public readonly int IndexBL_2 = 4;

        // Curve Controls
        public double EdgeAngleT { get; set; }
        public double EdgePercentT { get; set; }

        public double EdgeAngleB { get; set; }
        public double EdgePercentB { get; set; }

        // Only used if EndBL_2 is null
        public double B1AngleR { get; set; }
        public double B1PercentR { get; set; }

        public double B1AngleL { get; set; }
        public double B1PercentL { get; set; }

        // Only used if EndBL_2 is populated
        public double B2AngleR { get; set; }
        public double B2PercentR { get; set; }

        public double B2AngleL { get; set; }
        public double B2PercentL { get; set; }

        #region Public Methods

        public Point3D[] GetAllPoints()
        {
            return UtilityCore.Iterate<Point3D>(this.EndTL, this.EndTR, this.EndBR, this.EndBL_1, this.EndBL_2).ToArray();
        }

        public Curves_AxeSimple2 CloneNegateZ()
        {
            Curves_AxeSimple2 retVal = UtilityCore.Clone(this);

            retVal.EndTL = new Point3D(retVal.EndTL.X, retVal.EndTL.Y, retVal.EndTL.Z * -1);
            retVal.EndTR = new Point3D(retVal.EndTR.X, retVal.EndTR.Y, retVal.EndTR.Z * -1);
            retVal.EndBR = new Point3D(retVal.EndBR.X, retVal.EndBR.Y, retVal.EndBR.Z * -1);
            retVal.EndBL_1 = new Point3D(retVal.EndBL_1.X, retVal.EndBL_1.Y, retVal.EndBL_1.Z * -1);

            if (retVal.EndBL_2 != null)
            {
                retVal.EndBL_2 = new Point3D(retVal.EndBL_2.Value.X, retVal.EndBL_2.Value.Y, retVal.EndBL_2.Value.Z * -1);
            }

            return retVal;
        }

        #endregion

        #region Private Methods

        private void Define_Fixed()
        {
            Random rand = StaticRandom.GetRandomForThread();

            // Points
            this.EndTR = new Point3D(2, -1.1, 0);
            this.EndBR = new Point3D(1.3, 1.8, 0);

            this.EndTL = new Point3D(-1.5, -1, 0);

            this.EndBL_1 = new Point3D(-1.5, .5, 0);

            if (rand.NextBool())
            {
                this.EndBL_2 = this.EndBL_1;        // 2 is now the last point
                this.EndBL_1 = new Point3D(.3, 1.2, 0);
            }
            else
            {
                this.EndBL_2 = null;
            }

            // Curve Controls
            this.EdgeAngleT = 15;
            this.EdgePercentT = .3;

            this.EdgeAngleB = 15;
            this.EdgePercentB = .3;

            // Only used if EndBL_2 is null
            this.B1AngleR = 10;
            this.B1PercentR = .5;

            this.B1AngleL = 10;
            this.B1PercentL = .33;

            // Only used if EndBL_2 is populated
            this.B2AngleR = 70;
            this.B2PercentR = .6;

            this.B2AngleL = 70;
            this.B2PercentL = .4;
        }
        private void Define_Variable()
        {
            Random rand = StaticRandom.GetRandomForThread();

            #region Points

            // Edge
            this.EndTR = new Point3D(2, -1.1, 0) + Math3D.GetRandomVector_Circular(.25);
            this.EndBR = new Point3D(1.3, 1.8, 0) + Math3D.GetRandomVector_Circular(.25);

            // Left
            this.EndTL = new Point3D(-1.5, -1, 0);
            this.EndBL_1 = new Point3D(-1.5, .5, 0);

            if (rand.NextBool())
            {
                // Put an extra point along the bottom (left of this is that circle cutout)
                this.EndBL_2 = this.EndBL_1;        // 2 is now the last point
                this.EndBL_1 = new Point3D(.3, 1.2, 0) + Math3D.GetRandomVector_Circular(.25);
            }
            else
            {
                this.EndBL_2 = null;
            }

            if (this.EndBL_2 != null && rand.NextBool())
            {
                // Add a small beard
                this.EndBR += new Vector3D(0, rand.NextDouble(.25, 2.2), 0);
                this.EndBL_1 += new Vector3D(0, rand.NextDouble(.25, 1.8), 0);
            }

            double maxY = this.EndBR.Y - .25;

            if (this.EndBL_2 != null && this.EndBL_1.Y > maxY)
            {
                this.EndBL_1 = new Point3D(this.EndBL_1.X, maxY, this.EndBL_1.Z);       // can't let the middle point get lower, because the 3D would look wrong)
            }

            #endregion

            #region Curve Controls

            this.EdgeAngleT = rand.NextPercent(15, .25);
            this.EdgePercentT = rand.NextPercent(.3, .25);

            this.EdgeAngleB = rand.NextPercent(15, .25);
            this.EdgePercentB = rand.NextPercent(.3, .25);

            // Only used if EndBL_2 is null
            this.B1AngleR = rand.NextPercent(10, .25);
            this.B1PercentR = rand.NextPercent(.5, .25);

            this.B1AngleL = rand.NextPercent(10, .25);
            this.B1PercentL = rand.NextPercent(.33, .25);

            // Only used if EndBL_2 is populated
            //this.B2AngleR = 70;
            this.B2AngleR = rand.NextDouble(40, 80);
            this.B2PercentR = rand.NextPercent(.6, .25);

            //this.B2AngleL = 70;
            this.B2AngleL = rand.NextDouble(40, 80);
            this.B2PercentL = rand.NextPercent(.4, .25);

            #endregion
        }

        #endregion
    }

    #endregion
}
