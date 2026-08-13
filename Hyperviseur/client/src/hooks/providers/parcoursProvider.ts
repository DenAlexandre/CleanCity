import axios from 'axios';
import { useState } from 'react';
import { ParcoursModel } from '@/models'

type UseListParcoursProvider = {
  parcours: ParcoursModel[];
  loadParcours: () => void;
};

type ListParcoursInput = {
  dateStart?: string;
  dateEnd?: string;
  parcoursID?: string;
};

export const useListParcours = (input?: ListParcoursInput): UseListParcoursProvider => {
  const [parcours, setParcours] = useState<ParcoursModel[]>([]);

  function setAxiosData(parcoursData: ParcoursModel[]): void {
    setParcours(parcoursData);
  }

  const fetchParcours = (): void => {
    axios
      .get(`http://localhost:3000/parcours`)
      .then((data) => setAxiosData(data.data as ParcoursModel[]));
  };

  return { parcours, loadParcours: fetchParcours };
};

type ParcoursProviders = {
  useListParcours: (input?: ListParcoursInput) => UseListParcoursProvider;
};

export const UseParcoursProvider = (): ParcoursProviders => ({
    useListParcours,
});