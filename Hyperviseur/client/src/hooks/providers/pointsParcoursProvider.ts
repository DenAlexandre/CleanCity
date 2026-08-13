import axios from 'axios';
import { useState } from 'react';
import { PointsParcoursModel } from '@/models'

type UseListParcoursProvider = {
  pointsParcours: PointsParcoursModel[];
  loadPointParcours: () => void;
};

type ListPointsParcoursInput = {
  dateStart?: string;
  dateEnd?: string;
  parcoursID?: string;
};

export const useListPointsParcours = (input?: ListPointsParcoursInput): UseListParcoursProvider => {
  const [pointsParcours, setPointsParcours] = useState<PointsParcoursModel[]>([]);

  function setAxiosData(pointsParcoursData: PointsParcoursModel[]): void {
    /*if (input?.dateStart != undefined && input?.dateEnd != undefined) {
      const start = new Date(input?.dateStart as string);
      const end = new Date(input?.dateEnd as string);

      pointsParcoursData = pointsParcoursData.filter((pointParcours) => {
        const pointParcoursDate = new Date(pointParcours.timestamp);

        return pointParcoursDate >= start && pointParcoursDate <= end;
      });
    }*/

    setPointsParcours(pointsParcoursData);
  }

  const fetchPointsParcours = (): void => {
    axios
      .get(`http://localhost:3000/points_parcours`)
      .then((data) => setAxiosData(data.data as PointsParcoursModel[]));
    //.then((data) => setPointsParcours(data.data));
  };

  return { pointsParcours, loadPointParcours: fetchPointsParcours };
};

type PointsParcoursProviders = {
  useListPointsParcours: (input?: ListPointsParcoursInput) => UseListParcoursProvider;
};

export const UsePointsParcoursProvider = (): PointsParcoursProviders => ({
  useListPointsParcours,
});