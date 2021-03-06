﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Gekko
{
    public class Matrix : IVariable
    {
        //Abstract class containing a Matrix                

        public double[,] data = null;

        public Matrix()
        {
            this.data = null;
        }

        public Matrix(int n1, int n2)
        {
            this.data = new double[n1, n2];
        }

        public Matrix(int n1, int n2, double d)
        {
            this.data = new double[n1, n2];
            for (int i = 0; i < n1; i++)
            {
                for (int j = 0; j < n2; j++)
                {
                    this.data[i, j] = d;
                }
            }
        }

        public IVariable Indexer(IVariable index1, IVariable index2, GekkoTime t)
        {
            int i1 = O.GetInt(index1);
            int i2 = O.GetInt(index2);            
            try
            {
                double d = this.data[i1 - 1, i2 - 1];            
                return new ScalarVal(d);
            }
            catch (System.IndexOutOfRangeException e)  // CS0168
            {
                HandleIndexOutOfRange(index1, index2);
                throw new GekkoException();
            }
        }

        public IVariable Indexer(IVariable index1, GekkoTime t)
        {
            int i1 = O.GetInt(index1);
            int d1 = this.data.GetLength(0);
            int d2 = this.data.GetLength(1);            
            if (d2 == 1)
            {
                //1 col: column vector
                IVariable one = new ScalarVal(1d);
                return Indexer(index1, one, t);  //we implicitly understand #a[3] as #a[3,1] here. But we cannot do the inverse on a row vector.
            }
            G.Writeln("*** ERROR: You are trying to use [" + i1 + "] on a " + d1 + "x" + d2 + " matrix");
            G.Writeln("           This notation can only be used regarding nx1 matrices (column vectors)");
            throw new GekkoException();
        }

        public IVariable Indexer(IVariablesFilterRange indexRange, GekkoTime t)
        {
            int d1 = this.data.GetLength(0);
            int d2 = this.data.GetLength(1);
            if (d2 == 1)
            {
                return Indexer(indexRange, new ScalarVal(1d), t);                
            }
            G.Writeln("*** ERROR: You are trying to use [ .. ] on a " + d1 + "x" + d2 + " matrix");
            G.Writeln("           This notation can only be used regarding nx1 matrices (column vectors)");
            throw new GekkoException();
        }

        public IVariable Indexer(IVariablesFilterRange indexRange1, IVariablesFilterRange indexRange2, GekkoTime t)
        {
            int i1 = 1;
            int i2 = this.data.GetLength(0);
            int j1 = 1;
            int j2 = this.data.GetLength(1);
            if (indexRange1.first != null) i1 = O.GetInt(indexRange1.first);
            if (indexRange1.last != null) i2 = O.GetInt(indexRange1.last);
            if (indexRange2.first != null) j1 = O.GetInt(indexRange2.first);
            if (indexRange2.last != null) j2 = O.GetInt(indexRange2.last);
            if (i1 > i2)
            {
                G.Writeln2("*** ERROR: Range " + i1 + ".." + i2 + " is descending");
                throw new GekkoException();                
            }
            if (j1 > j2)
            {
                G.Writeln2("*** ERROR: Range " + j1 + ".." + j2 + " is descending");
                throw new GekkoException();
            }
            try
            {
                Matrix m = new Matrix(i2 - i1 + 1, j2 - j1 + 1);
                int ii1 = i1 - 1;
                int jj1 = j1 - 1;
                for (int i = i1 - 1; i <= i2 - 1; i++)
                {
                    for (int j = j1 - 1; j <= j2 - 1; j++)
                    {
                        m.data[i - ii1, j - jj1] = this.data[i, j];
                    }
                }
                return m;
            }
            catch (System.IndexOutOfRangeException e)  // CS0168
            {
                G.Writeln("*** ERROR: Index out of range: [" + i1 + " .. " + i2 + ", " + j1 + " .. " + j2 + " ]");
                if (i1 == 0 || i2 == 0 || j1 == 0 || j2 == 0) G.Writeln("           Please note that indicies are 1-based");
                throw new GekkoException();
            }
        }
        
        public IVariable Indexer(IVariable index, IVariablesFilterRange indexRange, GekkoTime t)
        {
            int i0 = O.GetInt(index);            
            int j1 = 1;
            int j2 = this.data.GetLength(1);            
            if (indexRange.first != null) j1 = O.GetInt(indexRange.first);
            if (indexRange.last != null) j2 = O.GetInt(indexRange.last);
            try
            {
                Matrix m = new Matrix(1, j2 - j1 + 1);

                int jj1 = j1 - 1;
                int i = i0 - 1;

                for (int j = j1 - 1; j <= j2 - 1; j++)
                {
                    m.data[0, j - jj1] = this.data[i, j];
                }

                return m;
            }
            catch (System.IndexOutOfRangeException e)  // CS0168
            {
                G.Writeln("*** ERROR: Index out of range: [" + i0 + ", " + j1 + " .. " + j2 + " ]");
                if (i0 == 0 || j1 == 0 || j2 == 0) G.Writeln("           Please note that indicies are 1-based");
                throw new GekkoException();
            }
        }

        public IVariable Indexer(IVariablesFilterRange indexRange, IVariable index, GekkoTime t)
        {
            int i1 = 1;
            int i2 = this.data.GetLength(0);            
            if (indexRange.first != null) i1 = O.GetInt(indexRange.first);
            if (indexRange.last != null) i2 = O.GetInt(indexRange.last);
            if (i1 > i2)
            {
                G.Writeln2("*** ERROR: Range " + i1 + ".." + i2 + " is descending");
                throw new GekkoException();
            }
            int j0 = O.GetInt(index);
            try
            {
                Matrix m = new Matrix(i2 - i1 + 1, 1);
                int ii1 = i1 - 1;
                int j = j0 - 1;
                for (int i = i1 - 1; i <= i2 - 1; i++)
                {
                    
                        m.data[i - ii1, 0] = this.data[i, j];
                    
                }
                return m;
            }
            catch (System.IndexOutOfRangeException e)  // CS0168
            {
                G.Writeln("*** ERROR: Index out of range: [" + i1 + " .. " + i2 + ", " + j0 + "]");
                if (i1 == 0 || i2 == 0 || j0 == 0) G.Writeln("           Please note that indicies are 1-based");
                throw new GekkoException();
            }
        }

        public IVariable Negate(GekkoTime t)
        {
            int ni = this.data.GetLength(0);
            int nj = this.data.GetLength(1);            
            Matrix m = new Matrix(ni, nj);
            for (int i = 0; i < ni; i++)
            {
                for (int j = 0; j < nj; j++)
                {
                    m.data[i, j] = -this.data[i, j];
                }
            }
            return m;               
        }

        public void InjectAdd(IVariable x, IVariable y, GekkoTime t)
        {
            G.Writeln2("*** ERROR: #8703458724");
            throw new GekkoException();
        }

        public double GetVal(GekkoTime t)
        {
            if (this.data.GetLength(0) == 1 && this.data.GetLength(1) == 1)
            {
                return this.data[0, 0];
            }
            else
            {
                G.Writeln2("*** ERROR: Type mismatch: you are trying to extract a VAL from a matrix.");
                G.Writeln("           Maybe you need an [x, y]-indexer on the matrix, for instance #a[2, 3]?");
            }
            throw new GekkoException();
        }        

        public string GetString()
        {            
            G.Writeln2("*** ERROR: Type mismatch: you are trying to extract a STRING from a matrix.");
            throw new GekkoException();
        }

        public GekkoTime GetDate(O.GetDateChoices c)
        {
            G.Writeln2("*** ERROR: Type mismatch: you are trying to extract a DATE from a matrix.");
            throw new GekkoException();
        }

        public List<string> GetList()
        {
            G.Writeln2("*** ERROR: Type mismatch: you are trying to extract a LIST from a matrix.");
            throw new GekkoException();
        }

        public EVariableType Type()
        {
            return EVariableType.Matrix;
        }

        public Matrix Clone()
        {
            int n1 = this.data.GetLength(0);
            int n2 = this.data.GetLength(1);
            Matrix m = new Matrix(n1, n2);
            for (int i = 0; i < n1; i++)
            {
                for (int j = 0; j < n2; j++)
                {
                    m.data[i, j] = this.data[i, j];
                }
            }
            return m;
        }

        public IVariable Add(IVariable x, GekkoTime t)
        {
            //clone this Add() method and do with O.SubtractMatrixScalar, also for Matrix and matrix            
            switch (x.Type())
            {
                case EVariableType.Matrix:
                    {
                        double[,] a = this.data;
                        double[,] b = ((Matrix)x).data;
                        int m = a.GetLength(0);
                        int k = a.GetLength(1);
                        if (b.GetLength(0) != m || b.GetLength(1) != k)
                        {
                            G.Writeln2("*** ERROR: The two matrices are not compatible for addition");
                            G.Writeln2("           " + m + "x" + k + " and " + b.GetLength(0) + "x" + b.GetLength(1) + " do not match");
                            throw new GekkoException();
                        }
                        double[,] c = O.AddMatrixMatrix(a, b, m, k);
                        Matrix z = new Matrix();
                        z.data = c;
                        return z;
                    }
                case EVariableType.Val:
                    {
                        //addition of a scalar is not legal, this is like AREMOS
                        G.Writeln2("*** ERROR: You cannot add a MATRIX and a VAL");
                        throw new GekkoException();
                    }
                default:
                    {                        
                        G.Writeln2("*** ERROR: You are trying to add a MATRIX and a " + x.Type().ToString().ToUpper());
                        throw new GekkoException();
                    }
            }
        }

        public IVariable Subtract(IVariable x, GekkoTime t)
        {
            //clone this Add() method and do with O.SubtractMatrixScalar, also for Matrix and matrix            
            EVariableType type = x.Type();
            if (type == EVariableType.Matrix)
            {
                double[,] a = this.data;
                double[,] b = ((Matrix)x).data;
                int m = a.GetLength(0);
                int k = a.GetLength(1);
                if (b.GetLength(0) != m || b.GetLength(1) != k)
                {
                    G.Writeln2("*** ERROR: The two matrices are not compatible for subtraction");
                    G.Writeln2("           " + m + "x" + k + " and " + b.GetLength(0) + "x" + b.GetLength(1) + " do not match");
                    throw new GekkoException();
                }
                double[,] c = O.SubtractMatrixMatrix(a, b, m, k);
                Matrix z = new Matrix();
                z.data = c;
                return z;
            }
            else             
            {
                //subtraction of a scalar is not legal, this is like AREMOS
                G.Writeln2("*** ERROR: You are trying to add a MATRIX and a " + type.ToString().ToUpper());
                throw new GekkoException();
            }
        }

        public IVariable Multiply(IVariable x, GekkoTime t)
        {
            EVariableType type = x.Type();
            if (type == EVariableType.Matrix)
            {
                double[,] a = this.data;
                double[,] b = ((Matrix)x).data;
                // m x k  *  p x n
                int m = a.GetLength(0);                
                int k = a.GetLength(1);
                int p = b.GetLength(0);
                int n = b.GetLength(1);

                if (p == 1 && n == 1)
                {
                    //Special case, #a * #onebyone
                    double[,] c = O.MultiplyMatrixScalar(a, b[0, 0], m, k);
                    Matrix z = new Matrix();
                    z.data = c;
                    return z;
                }
                else if (m == 1 && k == 1)
                {
                    //Special case,  #onebyone * #a
                    double[,] c = O.MultiplyMatrixScalar(b, a[0, 0], p, n);
                    Matrix z = new Matrix();
                    z.data = c;
                    return z;
                }
                else
                {

                    if (k != p)
                    {
                        G.Writeln2("*** ERROR: The two matrices are not compatible for multiplication");
                        G.Writeln2("           " + m + "x" + k + " and " + b.GetLength(0) + "x" + b.GetLength(1) + " do not match");
                    }
                    double[,] c = new double[m, n];
                    alglib.rmatrixgemm(m, n, k, 1, a, 0, 0, 0, b, 0, 0, 0, 0, ref c, 0, 0);
                    Matrix z = new Matrix();
                    z.data = c;
                    return z;
                    //c = {{74, 80, 86, 92}, {173, 188, 203, 218}}
                }
            }
            else if (type == EVariableType.Val)
            {
                //This is allowed in AREMOS, too
                double[,] a = this.data;
                double b = O.GetVal(x, t);
                int m = a.GetLength(0);                
                int k = a.GetLength(1);
                double[,] c = O.MultiplyMatrixScalar(a, b, m, k);
                Matrix z = new Matrix();
                z.data = c;
                return z;
            }
            else
            {
                G.Writeln2("*** ERROR: You are trying to multiply a MATRIX and a " + type.ToString().ToUpper());
                throw new GekkoException();
            }
        }

        public IVariable Divide(IVariable x, GekkoTime t)
        {
            switch (x.Type())
            {

                case EVariableType.Matrix:
                    {
                        double[,] a = this.data;
                        double[,] b = ((Matrix)x).data;
                        // m x k  *  p x n
                        int m = a.GetLength(0);
                        int k = a.GetLength(1);
                        int p = b.GetLength(0);
                        int n = b.GetLength(1);

                        if (p == 1 && n == 1)
                        {
                            //Special case
                            double[,] c = O.MultiplyMatrixScalar(a, 1d/b[0, 0], m, k);
                            Matrix z = new Matrix();
                            z.data = c;
                            return z;
                        }
                        else
                        {
                            G.Writeln2("*** ERROR: You can only divide a matrix with a scalar or 1x1 matrix.");
                            throw new GekkoException();                            
                        }
                    }
                case EVariableType.Val:
                    {
                        double[,] a = this.data;
                        double b = O.GetVal(x, t);
                        int m = a.GetLength(0);
                        int k = a.GetLength(1);
                        double[,] c = O.MultiplyMatrixScalar(a, 1d/b, m, k);
                        Matrix z = new Matrix();
                        z.data = c;
                        return z;
                    }
                default:
                    {
                        G.Writeln2("*** ERROR: You can only divide a matrix with a scalar or 1x1 matrix.");
                        throw new GekkoException();
                    }
            }
        }

        public IVariable Power(IVariable x, GekkoTime t)
        {
            G.Writeln2("*** ERROR: You cannot use power function with matrices");
            throw new GekkoException();
        }

        public void SetData(IVariable x1, IVariable x2, IVariable x3)
        {
            try
            {
                this.data[O.GetInt(x1) - 1, O.GetInt(x2) - 1] = O.GetVal(x3, Globals.tNull);
            }
            catch (IndexOutOfRangeException e)
            {
                HandleIndexOutOfRange(x1, x2);
                throw new GekkoException();
            }
        }

        public IVariable SetData(IVariable x1, IVariablesFilterRange indexRange2, IVariable x3)
        {
            //Does not need to be so fast, so we use this trick
            IVariablesFilterRange range = new Gekko.IVariablesFilterRange(x1, x1);
            return this.SetData(range, indexRange2, x3);            
        }

        public IVariable SetData(IVariablesFilterRange indexRange1, IVariable x2, IVariable x3)
        {
            //Does not need to be so fast, so we use this trick
            IVariablesFilterRange range = new Gekko.IVariablesFilterRange(x2, x2);
            return this.SetData(indexRange1, range, x3);
        }

        public IVariable SetData(IVariablesFilterRange indexRange1, IVariablesFilterRange indexRange2, IVariable x3)
        {
            int i1 = 1;
            int i2 = this.data.GetLength(0);
            int j1 = 1;
            int j2 = this.data.GetLength(1);
            if (indexRange1.first != null) i1 = O.GetInt(indexRange1.first);
            if (indexRange1.last != null) i2 = O.GetInt(indexRange1.last);
            if (indexRange2.first != null) j1 = O.GetInt(indexRange2.first);
            if (indexRange2.last != null) j2 = O.GetInt(indexRange2.last);
            if (i1 > i2)
            {
                G.Writeln2("*** ERROR: Range " + i1 + ".." + i2 + " is descending");
                throw new GekkoException();
            }
            if (j1 > j2)
            {
                G.Writeln2("*** ERROR: Range " + j1 + ".." + j2 + " is descending");
                throw new GekkoException();
            }

            if (x3.Type() == EVariableType.Matrix)
            {
                Matrix m = (Matrix)x3;
                int dimI = i2 - i1 + 1;
                int dimJ = j2 - j1 + 1;
                if(dimI != m.data.GetLength(0) || dimJ != m.data.GetLength(1))
                {
                    G.Writeln2("*** ERROR: Left-hand side selection is " + dimI + "x" + dimJ + ", but right-hand matrix is " + m.data.GetLength(0) + "x" + m.data.GetLength(1));
                    throw new GekkoException();
                }

                try
                {
                    int ii1 = i1 - 1;
                    int jj1 = j1 - 1;
                    for (int i = i1 - 1; i <= i2 - 1; i++)
                    {
                        for (int j = j1 - 1; j <= j2 - 1; j++)
                        {
                            this.data[i, j] = m.data[i - ii1, j - jj1];
                        }
                    }
                    return this;
                }
                catch (System.IndexOutOfRangeException e)  // CS0168
                {
                    G.Writeln("*** ERROR: Left-side index out of range: [" + i1 + " .. " + i2 + ", " + j1 + " .. " + j2 + " ]");
                    if (i1 == 0 || i2 == 0 || j1 == 0 || j2 == 0) G.Writeln("           Please note that indicies are 1-based");
                    throw new GekkoException();
                }
            }
            else if(x3.Type() == EVariableType.Val)
            {
                ScalarVal v = (ScalarVal)x3;
                try
                {
                    int ii1 = i1 - 1;
                    int jj1 = j1 - 1;
                    for (int i = i1 - 1; i <= i2 - 1; i++)
                    {
                        for (int j = j1 - 1; j <= j2 - 1; j++)
                        {
                            this.data[i, j] = v.val;
                        }
                    }
                    return this;
                }
                catch (System.IndexOutOfRangeException e)  // CS0168
                {
                    G.Writeln("*** ERROR: Left-side index out of range: [" + i1 + " .. " + i2 + ", " + j1 + " .. " + j2 + " ]");
                    if (i1 == 0 || i2 == 0 || j1 == 0 || j2 == 0) G.Writeln("           Please note that indicies are 1-based");
                    throw new GekkoException();
                }
            }
            else
            {
                G.Writeln2("*** ERROR: Expected right-hand side to be a matrix or a scalar");
                throw new GekkoException();
            }
        }

        private void HandleIndexOutOfRange(IVariable x1, IVariable x2)
        {
            int dim1 = O.GetInt(x1);
            int dim2 = O.GetInt(x2);
            int maxDim1 = this.data.GetLength(0);
            int maxDim2 = this.data.GetLength(1);
            if (dim1 > maxDim1 || dim2 > maxDim2)
            {
                G.Writeln2("*** ERROR: Indexer [" + dim1 + ", " + dim2 + "] on a matrix with dimensions " + maxDim1 + " x " + maxDim2);                
            }
            if (dim1 < 1 || dim2 < 1)
            {
                G.Writeln2("*** ERROR: Indexer [" + dim1 + ", " + dim2 + "]: indexers must be >= 1");                
            }            
        }

    }
}
