﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OMDb.Core.Models
{
    /// <summary>
    /// 评分
    /// </summary>
    public class Rating
    {
        /// <summary>
        /// 最大分
        /// </summary>
        public double Max { get; set; }
        /// <summary>
        /// 最小分
        /// </summary>
        public double Min { get; set; } = 0;
        /// <summary>
        /// 评分
        /// </summary>
        public double Rate { get; set; }
        /// <summary>
        /// 扩展信息
        /// </summary>
        public string Remarks { get; set; }
        public Rating(double rate, double max, string remarks = null)
        {
            Max = max;
            Rate = rate;
            Remarks = remarks;
        }
        public Rating(double rate, double max, double min, string remarks = null)
        {
            Max = max;
            Rate = rate;
            Rate = rate;
            Remarks = remarks;
        }
        public override string ToString()
        {
            return $"{Rate}/{Max}({Remarks})";
        }
    }
}
