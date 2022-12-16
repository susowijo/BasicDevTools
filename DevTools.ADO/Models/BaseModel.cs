using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevTools.ADO.Models
{
    internal class BaseModel
    {
        /// <summary>
        /// Represents the id of the entity
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// Represents the created date of the entity
        /// </summary>
        public DateTime? CreateOn { get; set; }

        /// <summary>
        /// Represents the updated date of the entity
        /// </summary>
        public DateTime? UpdateOn { get; set; }
    }

    public enum OperationType
    {
        /// <summary>
        /// Insert
        /// </summary>
        [Description("Insert element")]
        Insert = 1,

        /// <summary>
        /// Update
        /// </summary>
        [Description("Update element")]
        Update = 2,
    }
}
