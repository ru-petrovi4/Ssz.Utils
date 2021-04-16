using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataGrpc.Server
{
    public sealed partial class ElementValueJournalsCollection
    {
        #region public functions

        public void CombineWith(ElementValueJournalsCollection nextElementValueJournalsCollection)
        {
            Guid = nextElementValueJournalsCollection.Guid;
            NextCollectionGuid = nextElementValueJournalsCollection.NextCollectionGuid;

            for (int i = 0; i < nextElementValueJournalsCollection.ElementValueJournals.Count; i++)
            {
                ElementValueJournal elementValueJournal;
                if (i < ElementValueJournals.Count)
                {
                    elementValueJournal = ElementValueJournals[i];
                }
                else
                {
                    elementValueJournal = new ElementValueJournal();
                    ElementValueJournals.Add(elementValueJournal);
                }
                elementValueJournal.CombineWith(nextElementValueJournalsCollection.ElementValueJournals[i]);
            }            
        }

        #endregion
    }
}
