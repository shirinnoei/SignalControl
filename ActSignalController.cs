﻿using System.Collections.Generic;
using SwashSim_VehControlPoint;
using SwashSim_VehicleDetector;



namespace SwashSim_SignalControllerActuated
{
    public class SignalControllerActuated
    {
        uint _id;
        protected ControllerPhases _phases;        
        InterGreens _interGreens;
        List<TimingPlan> _timingPlans;
        TimingPlan _activeTimingPlan;
        VehicleControlPoints _vehicleControlPoints;
        Detectors _detectors;

        protected double _elapsedSimTime;
        private List<SignalStatusConvertor> _convertors;

        public int ID
        {
            get { return (int)_id; }
        }

        public ControllerPhases Phases
        {
            get { return _phases; }
        }

        public TimingPlan ActiveTimingPlan
        {
            get { return _activeTimingPlan; }
            set { _activeTimingPlan = value; }
        }

        public Detectors Detectors
        {
            get { return _detectors; }
        }

        public VehicleControlPoints VehicleControlPoints
        {
            get { return _vehicleControlPoints; }
        }

        public InterGreens InterGreens
        {
            get { return _interGreens; }
        }

        public List<TimingPlan> TimingPlans { get => _timingPlans; set => _timingPlans = value; }

        public SignalControllerActuated(uint ID)
        {
            _id = ID;
            _elapsedSimTime = 0;
            _phases = new ControllerPhases();
            _interGreens = new InterGreens(ref _phases);
            _timingPlans = new List<TimingPlan>();
            _vehicleControlPoints = new VehicleControlPoints();
            _detectors = new Detectors();
            _convertors = new List<SignalStatusConvertor>();
        }

        public virtual void LoadTimingPlan()
        { }

        public void AddDetector(DetectorData Detector)
        {
            this._detectors.Add(Detector);
        }

        public void AddTimingPlan(TimingPlan TimingPlan)
        {
            this._timingPlans.Add(TimingPlan);
        }

        public void AddControlPoint(VehicleControlPointData ControlPoint)
        {
            this._vehicleControlPoints.Add(ControlPoint);
        }

        public void SwitchPhase(uint FromPhaseID, uint ToPhaseID)
        {
            if (FromPhaseID == 0 || ToPhaseID == 0) return;
            _convertors.Add(new SignalStatusConvertor(FromPhaseID, ToPhaseID, _elapsedSimTime, (double)Phases.GetByID(FromPhaseID).TimingPlanParameters.Yellow, InterGreens.GetInterGreenValue(FromPhaseID, ToPhaseID).InterGreenValue));
        }

        public void UpdateSimTime(double ElapsedSimTime)
        {
            this._elapsedSimTime = ElapsedSimTime;
        }

        public void UpdateSignalStatus()
        {
            int i = 0;
            while (i < _convertors.Count)
            {
                SignalStatusConvertor convertor = _convertors[i];
                if (!convertor.InProcess(Phases, _elapsedSimTime))
                {
                    _convertors.Remove(convertor);
                }
                i++;
            }
        }

        public virtual void UpdateLogic()
        {
            //Do not delete; this function is overridden in dual-ring controller class
        }

        public virtual void UpdateControlPoints()
        {
            foreach (ControllerPhase phase in this.Phases)
            {
                VehicleControlPoints controlPoints = new VehicleControlPoints();
                foreach (uint VCID in phase.TimingPlanParameters.AssociatedControlPointIds)
                {
                    controlPoints.Add(_vehicleControlPoints.GetByID(VCID));
                }
                foreach (VehicleControlPointData controlPoint in controlPoints)
                {
                    if (phase.Status == SignalStatus.Green)
                        controlPoint.DisplayIndication = ControlDisplayIndication.Green;
                    if (phase.Status == SignalStatus.Yellow)
                        controlPoint.DisplayIndication = ControlDisplayIndication.Yellow;
                    if (phase.Status == SignalStatus.Red)
                        controlPoint.DisplayIndication = ControlDisplayIndication.Red;
                }
            }
        }

        public void RecordPhaseStatus()
        {
            foreach (ControllerPhase phase in this._phases)
            {
                phase.StatusRecord.Add(phase.Status.ToString("g"));
            }
        }

        public void RecordVCPStatus()
        {
            foreach (VehicleControlPointData vcp in this._vehicleControlPoints)
            {
                //add a Lint<string> record into VCP class as is done in ControllerPhase class.
                //then add a row as below:
                //vcp.StatusRecord.Add(vcp.DisplayIndication.ToString("g"));
            }
        }

        public void Run(double ElapsedSimTime)
        {
            UpdateSimTime(ElapsedSimTime);
            UpdateLogic();
            UpdateSignalStatus();
            UpdateControlPoints();
        }

        public void Run(double ElapsedSimTime, bool IsRecordPhaseStatus, bool IsRecordVCPStatus)
        {
            this.Run(ElapsedSimTime);
            if (IsRecordPhaseStatus)
            {
                RecordPhaseStatus();
            }
            if (IsRecordVCPStatus)
            {
                RecordVCPStatus();
            }
        }
    }

    public class ControllerPhase
    {
        uint _id;
        SignalStatus _status;
        Detectors _detectors;
        double _activeElapsedSimTime;
        double _desiredEndSimTime;
        TimingPlanPhase _timingPlanParameters;
        List<string> _statusRecord;

        public ControllerPhase(uint ID, TimingPlanPhase TimingPlanParameters, Detectors Detectors)
        {
            _id = ID;
            _status = SignalStatus.Red;
            _activeElapsedSimTime = 0;
            _desiredEndSimTime = 0;
            _timingPlanParameters = TimingPlanParameters;
            _detectors = new Detectors();
            _statusRecord = new List<string>();
            foreach (DetectorData detector in Detectors)
            {
                foreach (byte detectorID in TimingPlanParameters.AssociatedDetectorIds)
                {
                    if (detectorID == detector.Id)
                    {
                        _detectors.Add(detector);
                    }
                }
            }
        }

        public uint ID
        {
            get { return _id; }
        }

        public SignalStatus Status
        {
            get { return _status; }
            set { _status = value; }
        }

        public Detectors Detectors
        {
            get { return _detectors; }
        }

        public List<string> StatusRecord
        {
            get { return _statusRecord; }
        }

        public TimingPlanPhase TimingPlanParameters
        {
            get { return this._timingPlanParameters; }
        }

        public double DesiredDuration
        {
            get { return _desiredEndSimTime - _activeElapsedSimTime; }
        }

        public double ActiveDuration(double ElapsedSimTime)
        {
            if (PhaseActive)
            {
                return ElapsedSimTime - _activeElapsedSimTime;
            }
            else
            {
                return 0;
            }
        }

        public void SetDesiredPhaseEnd(double DesiredPhaseEndSimTime)
        {
            this._desiredEndSimTime = DesiredPhaseEndSimTime;
        }

        public void SetPhaseStartTime(double PhaseStartTime)
        {
            _activeElapsedSimTime = PhaseStartTime;
        }

        public bool PhaseActive
        {
            get
            {
                if (_status == SignalStatus.Green || _status == SignalStatus.GreenFlash)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }

    public class ControllerPhases : List<ControllerPhase>
    {
        public ControllerPhase GetByID(uint ID)
        {
            foreach (ControllerPhase temp in this)
            {
                if (temp.ID == ID)
                {
                    return temp;
                }
            }
            return null;
        }
    }

    public class SignalStatusConvertor
    {
        private uint _endingPhaseID;
        private uint _startingPhaseID;
        private double _greenEnd;
        private double _yellowEnd;
        private double _redEnd;

        public SignalStatusConvertor(uint EndingPhase, uint StartingPhase, double GreenEndTime, double YellowDuration, double InterGreenDuration)
        {
            _endingPhaseID = EndingPhase;
            _startingPhaseID = StartingPhase;
            _greenEnd = GreenEndTime;
            _yellowEnd = _greenEnd + YellowDuration;
            _redEnd = GreenEndTime + InterGreenDuration;
        }

        public bool InProcess(ControllerPhases Phases, double ElapsedSimTime)
        {
            if (ElapsedSimTime >= _redEnd)
            {
                Phases.GetByID(_startingPhaseID).Status = SignalStatus.Green;
                Phases.GetByID(_startingPhaseID).SetPhaseStartTime(ElapsedSimTime);
                return false;
            }

            if (ElapsedSimTime >= _greenEnd)
            {
                Phases.GetByID(_endingPhaseID).Status = SignalStatus.Yellow;
            }
            if (ElapsedSimTime >= _yellowEnd)
            {
                Phases.GetByID(_endingPhaseID).Status = SignalStatus.Red;
            }

            return true;
        }
    }

    public class InterGreen
    {
        private uint _fromGroupID;
        private uint _toGroupID;
        private double _interGreen;

        public uint FromGroup
        {
            get { return _fromGroupID; }
        }

        public uint ToGroup
        {
            get { return _toGroupID; }
        }

        public double InterGreenValue
        {
            get { return _interGreen; }
            set { _interGreen = value; }
        }

        public InterGreen(uint FromGroup, uint ToGroup, double GreenSplitValue)
        {
            this._fromGroupID = FromGroup;
            this._toGroupID = ToGroup;
            this._interGreen = GreenSplitValue;
        }
    }

    public class InterGreens : List<InterGreen>
    {
        private ControllerPhases _phases;

        public InterGreens(ref ControllerPhases Phases)
            : base()
        {
            this._phases = Phases;
        }

        public double[,] InterGreenMatrix
        {
            get
            {
                if (_phases.Count == 0)
                {
                    return null;
                }
                double[,] matrix = new double[_phases.Count, _phases.Count];
                for (int i = 0; i < _phases.Count; i++)
                {
                    for (int j = 0; j < _phases.Count; j++)
                    {
                        matrix[i, j] = -1;
                    }
                }
                foreach (InterGreen temp in this)
                {
                    int i = _phases.IndexOf(_phases.GetByID(temp.FromGroup));
                    int j = _phases.IndexOf(_phases.GetByID(temp.ToGroup));
                    matrix[i, j] = temp.InterGreenValue;
                }
                return matrix;
            }
        }

        public InterGreen GetInterGreenValue(uint FromGroup, uint ToGroup)
        {
            foreach (InterGreen temp in this)
            {
                if (temp.FromGroup == FromGroup && temp.ToGroup == ToGroup)
                {
                    return temp;
                }
            }
            return null;
        }

        public void SetInterGreen(uint FromGroup, uint ToGroup, double GreenSplitValue)
        {
            InterGreen tempGreenSplit = GetInterGreenValue(FromGroup, ToGroup);
            if (tempGreenSplit == null)
            {
                if (GreenSplitValue == -1)
                {
                    return;
                }
                else
                {
                    this.Add(new InterGreen(FromGroup, ToGroup, GreenSplitValue));
                }
            }
            else
            {
                if (GreenSplitValue == -1)
                {
                    this.Remove(tempGreenSplit);
                }
                else
                {
                    tempGreenSplit.InterGreenValue = GreenSplitValue;
                }
            }
        }
    }

    public class Detectors : List<DetectorData>
    {
        public DetectorData GetByID(uint ID)
        {
            foreach (DetectorData tempDetector in this)
            {
                if (tempDetector.Id == (byte)ID)
                {
                    return tempDetector;
                }
            }
            return null;
        }

        public bool Call
        {
            get
            {
                bool detectorCall = false;
                foreach (DetectorData detector in this)
                {
                    if (detector.IsOccupied)
                        detectorCall = true;
                }
                return detectorCall;
            }
        }
    }

    public class VehicleControlPoints : List<VehicleControlPointData>
    {
        public VehicleControlPointData GetByID(uint ID)
        {
            foreach (VehicleControlPointData temp in this)
            {
                if (temp.Id == ID)
                {
                    return temp;
                }
            }
            return null;
        }
    }

    public enum SignalStatus
    {
        Red = 1,
        Yellow = 2,
        Green = 3,
        GreenFlash = 4,
        YellowFlash = 5,
        RedYellow = 6
    }
}