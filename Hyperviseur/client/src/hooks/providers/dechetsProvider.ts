import axios from 'axios';
import { useState } from 'react';
import { DechetModel } from '@/models'

type UseListDechetsProvider = {
  dechets: DechetModel[];
  loadDechet: () => void;
};

type ListDechetsInput = {
  dateStart?: string;
  dateEnd?: string;
  parcoursID?: number;
  bottleFilter?: boolean;
  canFilter?: boolean;
  overflowBinFilter?: boolean;
  garbageBagFilter?: boolean;
};

export const useListDechets = (input?: ListDechetsInput): UseListDechetsProvider => {
  const [dechets, setDechets] = useState<DechetModel[]>([]);

  function setAxiosData(dechetData: DechetModel[]): void {
    if (input?.dateStart != undefined && input?.dateEnd != undefined)
    {
      const start = new Date(input?.dateStart as string);
      const end = new Date(input?.dateEnd as string);
  
      dechetData = dechetData.filter((dechet) => 
        {
          const dechetDate = new Date(dechet.timestamp);
  
          return dechetDate >= start && dechetDate <= end;
      });
    }

    if (input?.parcoursID != undefined)
    {
      dechetData = dechetData.filter((dechet) => 
        {
          return dechet.parcoursID == input.parcoursID;
      });
    }

    if (input?.bottleFilter != undefined)
    {
      if (input.bottleFilter == false)
      {
        dechetData = dechetData.filter((dechet) => 
          {
            return dechet.dechetId != "bottle";
        });
      }
    }

    if (input?.canFilter != undefined)
      {
        if (input.canFilter == false)
        {
          dechetData = dechetData.filter((dechet) => 
            {
              return dechet.dechetId != "can";
          });
        }
      }

      if (input?.overflowBinFilter != undefined)
        {
          if (input.overflowBinFilter == false)
          {
            dechetData = dechetData.filter((dechet) => 
              {
                return dechet.dechetId != "overflow_bin";
            });
          }
        }

        if (input?.garbageBagFilter != undefined)
          {
            if (input.garbageBagFilter == false)
            {
              dechetData = dechetData.filter((dechet) => 
                {
                  return dechet.dechetId != "garbage_bag";
              });
            }
          }

    setDechets(dechetData);
  }

  const fetchDechets = (): void => {
    axios
      .get(`http://localhost:3000/dechets`)
      .then((data) => setAxiosData(data.data as DechetModel[]));
  };

  return { dechets, loadDechet: fetchDechets };
};

type DechetsProviders = {
  useListDechets: (input?: ListDechetsInput) => UseListDechetsProvider;
};

export const UseDechetsProvider = (): DechetsProviders => ({
    useListDechets,
});