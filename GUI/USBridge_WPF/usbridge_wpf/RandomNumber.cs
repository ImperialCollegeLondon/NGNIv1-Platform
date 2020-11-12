using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace USBridge_WPF
{

    /// <summary>
    /// Generates random numbers from both uniform and Gaussian distrubtions
    /// </summary>
    class RandomNumber
    {
        private static int gaussianN;
        private static double gaussianScaleFactor;
        private static Random rndGen;
        private static Object randLock;
        

        /// <summary>
        /// Initializes a new instance of the <see cref="RandomNumber"/> class.
        /// </summary>
        public RandomNumber()
        {
            rndGen = new Random(DateTime.Now.Millisecond);
            randLock = new Object();
            setGaussianAccuracy(6);
        }


        /// <summary>
        /// Returns a random number from a uniform distribution between 0.0 and 1.0.
        /// </summary>
        /// <returns></returns>
        public double randomUniform()
        {
            double r;
            lock (randLock)
            {
                r = rndGen.NextDouble();
            }
            return r;
        }


        /// <summary>
        /// Returns a random number from a uniform distribution between min and max.
        /// </summary>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        /// <returns></returns>
        public double randomUniform(double min, double max)
        {
            double r;
            lock (randLock)
            {
                r = rndGen.NextDouble();
            }
            return ((r * (max - min)) + min);
        }


        /// <summary>
        /// Returns a random number from a Gaussian distribution with variance = 1.0.
        /// This function relies on the central limit theorem to approximate a normal
        /// distribution.  Increasing gaussianN will improve accuracy at the expense of
        /// speed.  A value of 6 is adequate for most applications.        
        /// </summary>
        /// <returns></returns>
        public double randomGaussian()
        {
            double r = 0.0;
            for (int i = 0; i < gaussianN; ++i)
            {
                r += randomUniform(-1.0, 1.0);
            }
            r += gaussianScaleFactor;
            return r;
        }


        /// <summary>
        /// Sets the gaussian accuracy.
        /// Making n larger increases accuracy of Gaussian approximation at the expense of speed.
        /// </summary>
        /// <param name="n">The n.</param>
        public void setGaussianAccuracy(int n)
        {
            gaussianN = n;
            gaussianScaleFactor = Math.Sqrt(3.0) / Math.Sqrt(gaussianN);
        }
    }
}
