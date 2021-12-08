using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataGrpc.Server
{
    public sealed partial class ElementValuesJournalsCollection
    {
        #region public functions

        public void CombineWith(ElementValuesJournalsCollection nextElementValuesJournalsCollection)
        {
            Guid = nextElementValuesJournalsCollection.Guid;
            NextCollectionGuid = nextElementValuesJournalsCollection.NextCollectionGuid;

            for (int i = 0; i < nextElementValuesJournalsCollection.ElementValuesJournals.Count; i++)
            {
                ElementValuesJournal elementValuesJournal;
                if (i < ElementValuesJournals.Count)
                {
                    elementValuesJournal = ElementValuesJournals[i];
                }
                else
                {
                    elementValuesJournal = new ElementValuesJournal();
                    ElementValuesJournals.Add(elementValuesJournal);
                }
                elementValuesJournal.CombineWith(nextElementValuesJournalsCollection.ElementValuesJournals[i]);
            }            
        }

        #endregion
    }
}
