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

        public void Add(ElementValueJournalsCollection elementValueJournalsCollection)
        {
            Guid = elementValueJournalsCollection.Guid;
            NextCollectionGuid = elementValueJournalsCollection.NextCollectionGuid;

            for (int i = 0; i < elementValueJournalsCollection.ElementValueJournals.Count; i++)
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
                Add(elementValueJournal, elementValueJournalsCollection.ElementValueJournals[i]);
            }            
        }

        #endregion

        #region private functions

        private static void Add(ElementValueJournal thisElementValueJournal, ElementValueJournal elementValueJournal)
        {
            thisElementValueJournal.DoubleStatusCodes.Add(elementValueJournal.DoubleStatusCodes);
            thisElementValueJournal.DoubleTimestamps.Add(elementValueJournal.DoubleTimestamps);
            thisElementValueJournal.DoubleValues.Add(elementValueJournal.DoubleValues);

            thisElementValueJournal.UintStatusCodes.Add(elementValueJournal.UintStatusCodes);
            thisElementValueJournal.UintTimestamps.Add(elementValueJournal.UintTimestamps);
            thisElementValueJournal.UintValues.Add(elementValueJournal.UintValues);
        }

        #endregion
    }
}
