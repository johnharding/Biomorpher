using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biomorpher.IGA
{
    /// <summary>
    /// Some static things to help us out
    /// </summary>
    public static class Friends
    {
        //Function to get random number
        private static readonly Random getrandom = new Random();
        private static readonly object syncLock = new object();
        
        /// <summary>
        /// Returns a random integer within a domain
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static int GetRandomInt(int min, int max)
        {
            lock (syncLock)
            { // synchronize
                return getrandom.Next(min, max);
            }
        }

        /// <summary>
        /// Returns a double between [0.0, 1.0)
        /// </summary>
        /// <returns></returns>
        public static double GetRandomDouble()
        {
            lock (syncLock)
            { // synchronize
                return getrandom.NextDouble();
            }
        }
    }
}
