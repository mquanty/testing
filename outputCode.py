class LineCode(DSSClass):
    def __init__(self):
        self.SymComponentsChanged = False
        self.MatrixChanged = False
        self.Code = ""
    
    def Get_Code(self):
        return self.Code
    
    def Set_Code(self, value):
        self.Code = value
    
    def SetZ1Z0(self, i, value):
        self.SymComponentsChanged = True
        if i == 1:
            self.R1 = value
        elif i == 2:
            self.X1 = value
        elif i == 3:
            self.R0 = value
        elif i == 4:
            self.X0 = value
        elif i == 5:
            self.C1 = value
        elif i == 6:
            self.C0 = value
    
    def SetUnits(self, s):
        self.Units = self.GetUnitsCode(s)
    
    def DoMatrix(self, i):
        self.MatrixChanged = True
        matBuffer = []
        orderFound, nOrder = self.ParseAsSymMatrix(FNphases, matBuffer)
        if orderFound > 0:
            if i == 1:
                zValues = self.Z.GetValuesArrayPtr(nOrder)
                if nOrder == self.FNphases:
                    for j in range(self.FNphases * self.FNphases):
                        zValues[j].re = matBuffer[j]
            elif i == 2:
                zValues = self.Z.GetValuesArrayPtr(nOrder)
                if nOrder == self.FNphases:
                    for j in range(self.FNphases * self.FNphases):
                        zValues[j].im = matBuffer[j]
            elif i == 3:
                factor = TwoPi * self.BaseFrequency * 1.0e-9
                zValues = self.YC.GetValuesArrayPtr(nOrder)
                if nOrder == self.FNphases:
                    for j in range(self.FNphases * self.FNphases):
                        zValues[j].im = factor * matBuffer[j]
    
    def Edit(self):
        self.ActiveLineCodeObj = self.ElementList.Active
        self.ActiveDSSObject = self.ActiveLineCodeObj
        self.SymComponentsChanged = False
        self.MatrixChanged = False
        self.ActiveLineCodeObj.ReduceByKron = False
        paramPointer = 0
        paramName = self.Parser.NextParam()
        param = self.Parser.StrValue()
        while len(param) > 0:
            if len(paramName) == 0:
                paramPointer += 1
            else:
                paramPointer = self.CommandList.GetCommand(paramName)
            if paramPointer > 0 and paramPointer <= self.NumProperties:
                self.PropertyValue[paramPointer] = param
            if paramPointer == 0:
                self.DoSimpleMsg('Unknown parameter "' + paramName + '" for Object "' + self.Class_Name + '.' + self.Name + '"', 101)
            elif paramPointer == 1:
                self.Numphases = self.Parser.IntValue()
            elif paramPointer == 2:
                self.SetZ1Z0(1, self.Parser.Dblvalue())
            elif paramPointer == 3:
                self.SetZ1Z0(2, self.Parser.Dblvalue())
            elif paramPointer == 4:
                self.SetZ1Z0(3, self.Parser.Dblvalue())
            elif paramPointer == 5:
                self.SetZ1Z0(4, self.Parser.Dblvalue())
            elif paramPointer == 6:
                self.SetZ1Z0(5, self.Parser.Dblvalue() * 1.0e-9)
            elif paramPointer == 7:
                self.SetZ1Z0(6, self.Parser.Dblvalue() * 1.0e-9)
            elif paramPointer == 8:
                self.SetUnits(param)
            elif paramPointer == 9:
                self.DoMatrix(1)
            elif paramPointer == 10:
                self.DoMatrix(2)
            elif paramPointer == 11:
                self.DoMatrix(3)
            elif paramPointer == 12:
                self.BaseFrequency = self.Parser.DblValue()
            elif paramPointer == 13:
                self.NormAmps = self.Parser.Dblvalue()
            elif paramPointer == 14:
                self.EmergAmps = self.Parser.Dblvalue()
            elif paramPointer == 15:
                self.FaultRate = self.Parser.Dblvalue()
            elif paramPointer == 16:
                self.PctPerm = self.Parser.Dblvalue()
            elif paramPointer == 17:
                self.HrsToRepair = self.Parser.Dblvalue()
            elif paramPointer == 18:
                self.ReduceByKron = self.InterpretYesNo(param)
            elif paramPointer == 19:
                self.Rg = self.Parser.DblValue()
            elif paramPointer == 20:
                self.Xg = self.Parser.DblValue()
            elif paramPointer == 21:
                self.rho = self.Parser.DblValue()
            elif paramPointer == 22:
                self.FNeutralConductor = self.Parser.IntValue()
            elif paramPointer == 23:
                self.SetZ1Z0(5, self.Parser.Dblvalue() / (TwoPi * self.BaseFrequency) * 1.0e-6)
            elif paramPointer == 24:
                self.SetZ1Z0(6, self.Parser.Dblvalue() / (TwoPi * self.BaseFrequency) * 1.0e-6)
            else:
                self.ClassEdit(self.ActiveLineCodeObj, paramPointer - self.NumPropsThisClass)
            if paramPointer >= 9 and paramPointer <= 11:
                self.SymComponentsModel = False
            elif paramPointer == 18:
                if self.ReduceByKron and not self.SymComponentsModel:
                    self.DoKronReduction()
            paramName = self.Parser.NextParam()
            param = self.Parser.StrValue()
        if self.SymComponentsModel:
            self.CalcMatricesFromZ1Z0()
        if self.MatrixChanged:
            self.Zinv.Copyfrom(self.Z)
            self.Zinv.Invert()
    
    def MakeLike(self, LineName):
        otherLineCode = self.Find(LineName)
        if otherLineCode is not None:
            if self.FNPhases != otherLineCode.FNphases:
                self.FNPhases = otherLineCode.FNphases
                if self.Z is not None:
                    self.Z.Free()
                if self.Zinv is not None:
                    self.Zinv.Free()
                if self.Yc is not None:
                    self.Yc.Free()
                self.Z = TCmatrix.CreateMatrix(self.FNPhases)
                self.Zinv = TCMatrix.CreateMatrix(self.FNPhases)
                self.Yc = TCMatrix.CreateMatrix(self.FNPhases)
            self.Z.CopyFrom(otherLineCode.Z)
            self.Zinv.CopyFrom(otherLineCode.Zinv)
            self.Yc.CopyFrom(otherLineCode.Yc)
            self.BaseFrequency = otherLineCode.BaseFrequency
            self.R1 = otherLineCode.R1
            self.X1 = otherLineCode.X1
            self.R0 = otherLineCode.R0
            self.X0 = otherLineCode.X0
            self.C1 = otherLineCode.C1
            self.C0 = otherLineCode.C0
            self.Rg = otherLineCode.Rg
            self.Xg = otherLineCode.Xg
            self.rho = otherLineCode.rho
            self.FNeutralConductor = otherLineCode.FNeutralConductor
            self.NormAmps = otherLineCode.NormAmps
            self.EmergAmps = otherLineCode.EmergAmps
            self.FaultRate = otherLineCode.FaultRate
            self.PctPerm = otherLineCode.PctPerm
            self.HrsToRepair = otherLineCode.HrsToRepair
            for i in range(1, self.ParentClass.NumProperties + 1):
                self.PropertyValue[i] = otherLineCode.PropertyValue[i]
            return 1
        else:
            self.DoSimpleMsg('Error in Line MakeLike: "' + LineName + '" Not Found.', 102)
            return 0
    
    def Init(self, Handle):
        self.DoSimpleMsg('Need to implement TLineCode.Init', -1)
        return 0
    
    def Get_Rmatrix(self):
        result = "["
        for i in range(1, self.FNPhases + 1):
            for j in range(1, self.FNphases + 1):
                result += "{:12.8f} ".format(self.Z.GetElement(i, j).re)
            if i < self.FNphases:
                result += "|"
        result += "]"
        return result
    
    def Get_Xmatrix(self):
        result = "["
        for i in range(1, self.FNPhases + 1):
            for j in range(1, self.FNphases + 1):
                result += "{:12.8f} ".format(self.Z.GetElement(i, j).im)
            if i < self.FNphases:
                result += "|"
        result += "]"
        return result
    
    def Get_CMatrix(self):
        result = "["
        for i in range(1, self.FNPhases + 1):
            for j in range(1, self.FNphases + 1):
                result += "{:12.8f} ".format(self.Yc.GetElement(i, j).im / TwoPi / self.BaseFrequency * 1.0E9)
            if i < self.FNphases:
                result += "|"
        result += "]"
        return result
    
    def Set_NPhases(self, value):
        if value > 0:
            if self.FNPhases != value:
                self.FNPhases = value
                self.FNeutralConductor = self.FNphases
                self.CalcMatricesFromZ1Z0()
    
    def CalcMatricesFromZ1Z0(self):
        if self.Z is not None:
            self.Z.Free()
        if self.Zinv is not None:
            self.Zinv.Free()
        if self.Yc is not None:
            self.Yc.Free()
        self.Z = TCmatrix.CreateMatrix(self.FNphases)
        self.Zinv = TCMatrix.CreateMatrix(self.FNphases)
        self.Yc = TCMatrix.CreateMatrix(self.FNphases)
        oneThird = 1.0 / 3.0
        zTemp = CmulReal(cmplx(self.R1, self.X1), 2.0)
        zS = CmulReal(CAdd(zTemp, Cmplx(self.R0, self.X0)), oneThird)
        zM = CmulReal(Csub(cmplx(self.R0, self.X0), Cmplx(self.R1, self.X1)), oneThird)
        yC1 = TwoPi * self.BaseFrequency * self.C1
        yC0 = TwoPi * self.BaseFrequency * self.C0
        yS = CMulReal(Cadd(CMulReal(Cmplx(0.0, yC1), 2.0), Cmplx(0.0, yC0)), oneThird)
        yM = CmulReal(Csub(cmplx(0.0, yC0), Cmplx(0.0, yC1)), oneThird)
        for i in range(1, self.FNphases + 1):
            self.Z.SetElement(i, i, zS)
            self.Yc.SetElement(i, i, yS)
            for j in range(1, i):
                self.Z.SetElemsym(i, j, zM)
                self.Yc.SetElemsym(i, j, yM)
        self.Zinv.Copyfrom(self.Z)
        self.Zinv.Invert()
    
    def DumpProperties(self, F, Complete):
        self.Inherited.DumpProperties(F, Complete)
        F.write('~ nphases={}\n'.format(self.FNphases))
        F.write('~ r1={:.5g}\n'.format(self.R1))
        F.write('~ x1={:.5g}\n'.format(self.X1))
        F.write('~ r0={:.5g}\n'.format(self.R0))
        F.write('~ x0={:.5g}\n'.format(self.X0))
        F.write('~ c1={:.5g}\n'.format(self.C1 * 1.0e9))
        F.write('~ c0={:.5g}\n'.format(self.C0 * 1.0e9))
        F.write('~ {}\n'.format(self.LineUnitsStr(self.Units)))
        F.write('~ {}\n'.format(self.Get_Rmatrix()))
        F.write('~ {}\n'.format(self.Get_Xmatrix()))
        F.write('~ {}\n'.format(self.get_Cmatrix()))
        F.write('~ baseFreq={:.g}\n'.format(self.Basefrequency))
        F.write('~ reduceByKron={}\n'.format('Y' if self.ReduceByKron else 'N'))
        F.write('~ rg={:.5g}\n'.format(self.Rg))
        F.write('~ xg={:.5g}\n'.format(self.Xg))
        F.write('~ rho={:.5g}\n'.format(self.Rho))
        F.write('~ {}={}\n'.format(self.PropertyName[22], self.FNeutralConductor))
    
    def GetPropertyValue(self, Index):
        if Index == 1:
            return str(self.FNPhases)
        elif Index == 2:
            return '{:.5g}'.format(self.R1) if self.SymComponentsModel else '----'
        elif Index == 3:
            return '{:.5g}'.format(self.X1) if self.SymComponentsModel else '----'
        elif Index == 4:
            return '{:.5g}'.format(self.R0) if self.SymComponentsModel else '----'
        elif Index == 5:
            return '{:.5g}'.format(self.X0) if self.SymComponentsModel else '----'
        elif Index == 6:
            return '{:.5g}'.format(self.C1 * 1.0e9) if self.SymComponentsModel else '----'
        elif Index == 7:
            return '{:.5g}'.format(self.C0 * 1.0e9) if self.SymComponentsModel else '----'
        elif Index == 8:
            return self.LineUnitsStr(self.Units)
        elif Index == 9:
            return self.Get_Rmatrix()
        elif Index == 10:
            return self.Get_Xmatrix()
        elif Index == 11:
            return self.get_Cmatrix()
        elif Index == 12:
            return '{:.g}'.format(self.Basefrequency)
        elif Index == 18:
            return 'Y' if self.ReduceByKron else 'N'
        elif Index == 19:
            return '{:.5g}'.format(self.Rg)
        elif Index == 20:
            return '{:.5g}'.format(self.Xg)
        elif Index == 21:
            return '{:.5g}'.format(self.Rho)
        elif Index == 22:
            return str(self.FNeutralConductor)
        elif Index == 23:
            return '{:.5g}'.format(twopi * self.Basefrequency * self.C1 * 1.0e6) if self.SymComponentsModel else '----'
        elif Index == 24:
            return '{:.5g}'.format(twopi * self.Basefrequency * self.C0 * 1.0e6) if self.SymComponentsModel else '----'
        else:
            return self.Inherited.GetPropertyValue(Index)
    
    def InitPropertyValues(self, ArrayOffset):
        self.PropertyValue[1] = '3'
        self.PropertyValue[2] = '.058'
        self.Inherited.InitPropertyValues(self.NumPropsThisClass)
    
    def DoKronReduction(self):
        if self.FNeutralConductor == 0:
            return
        newZ = None
        newYC = None
        if self.FNphases > 1:
            try:
                newZ = self.Z.Kron(self.FNeutralConductor)
                self.YC.Invert()
                newYC = self.YC.Kron(self.FNeutralConductor)
            except Exception as e:
                self.DoSimpleMsg('Kron Reduction failed: LineCode.{}. Attempting to eliminate Neutral Conductor {}.'.format(self.Name, self.FNeutralConductor), 103)
            if newZ is not None and newYC is not None:
                newYC.Invert()
                self.FNphases = newZ.order
                self.Z.Free()
                self.YC.Free()
                self.Z = newZ
                self.YC = newYC
                self.FNeutralConductor = 0
                self.ReduceByKron = False
                self.PropertyValue[1] = str(self.FNPhases)
                self.PropertyValue[9] = self.Get_Rmatrix()
                self.PropertyValue[10] = self.Get_Xmatrix()
                self.PropertyValue[11] = self.get_Cmatrix()
            else:
                self.DoSimpleMsg('Kron Reduction failed: LineCode.{}. Attempting to eliminate Neutral Conductor {}.'.format(self.Name, self.FNeutralConductor), 103)
        else:
            self.DoSimpleMsg('Cannot perform Kron Reduction on a 1-phase LineCode: LineCode.{}'.format(self.Name), 103)


