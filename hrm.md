# 📚 STORED PROCEDURES - HỌC TÍNH LƯƠNG CHI TIẾT

**Mục đích**: Tài liệu này chọn lọc các stored procedures QUAN TRỌNG NHẤT từ `hrm.sql` để bạn học và hiểu chi tiết:
- Các trường dữ liệu (fields)
- Cách tính lương (calculation logic)
- Nghiệp vụ tính lương (business rules)

---

## 🎯 CÁC STORED PROCEDURE CHÍNH

### 1️⃣ **spGetPayrollBenefits** - Lấy Thông Tin Lương Cơ Bản
**Vị trí**: Lines 15173-15204  
**Mục đích**: Lấy thông tin lương cơ bản (benefit) của từng nhân viên

#### 📋 Các Trường Quan Trọng

```sql
-- Bảng: HR_tblPayrollBenefits (Lương cơ bản)
TotalSalary              -- Tổng lương (cho SS - sổ sách)
TotalSalaryNB            -- Tổng lương (cho NB - chi tiết)
TotalSalary85            -- Lương áp dụng 85% Rule (EffectiveDate)
BasicSalary              -- Lương cơ bản (để tính BHXH)
TravelSupportMoney       -- Hỗ trợ đi lại
PhoneMoneySupport        -- Hỗ trợ điện thoại
HousingSupportMoney      -- Hỗ trợ nhà ở
PriceSlideSupport        -- Phụ cấp trượt giá
HealthCareSupport        -- Hỗ trợ y tế
WorkSupport              -- Hỗ trợ làm việc
DirectCommandSupport     -- Phụ cấp chỉ đạo trực tiếp
VehicleCoordinationSupport -- Phụ cấp điều phối xe
EffectiveDate            -- Ngày hiệu lực lương mới (85% rule)
NoTax                    -- Cờ có cam kết không đóng thuế (1=có, 0=không)
ContractTypeID           -- Loại hợp đồng
SalaryPolicyID           -- Chính sách lương
```

#### 💡 Ý Nghĩa Business

- **TotalSalary vs TotalSalaryNB**: 
  - `TotalSalary`: Dùng cho **SS (Sổ sách)** - báo cáo kế toán
  - `TotalSalaryNB`: Dùng cho **NB (Chi tiết)** - chi tiết công thức tính từng khoản
  
- **TotalSalary85**: 
  - Nếu `EffectiveDate` trong tháng → tính pro-rata
  - 85% Rule: Nếu ngày hiệu lực >= 15% ngày trong tháng → dùng lương mới
  - Ví dụ: Tháng 30 ngày, tăng lương ngày 20 → 20/30 = 66.67% → dùng lương cũ toàn bộ
  - Ví dụ: Tăng lương ngày 25 → 25/30 = 83.33% < 85% → dùng lương cũ
  - Ví dụ: Tăng lương ngày 26 → 26/30 = 86.67% ≥ 85% → dùng lương mới toàn bộ

- **NoTax = 1**: 
  - Nhân viên thử việc có **cam kết không đóng thuế**
  - → Không áp dụng thuế lũy tiến → chỉ trừ 10% flat

---

### 2️⃣ **spUpdatePayroll** - Tính Lương Chi Tiết NB
**Vị trí**: Lines 21444-21565  
**Mục đích**: Tính toán lương chi tiết (NB - Nhân Biên) cho từng nhân viên

#### 🔢 Công Thức Tính

```sql
-- 1. Lương theo giờ (Hour Rate)
TotalSalaryByHour = IIF(IsSecurity=1,  
    ROUND(TotalSalary / @Day / 8, 2),           -- Bảo vệ: chia theo số ngày thực tế
    ROUND(TotalSalary / @NumberOfPayroll / 8, 2) -- Công nhân: chia theo số ngày công chuẩn
)

-- 2. Lương theo giờ 85% (cho EffectiveDate)
TotalSalaryByHour85 = IIF(IsSecurity=1,  
    ROUND(TotalSalary85 / @Day / 8, 2),
    ROUND(TotalSalary85 / @NumberOfPayroll / 8, 2)
)

-- 3. Lương OT tính theo mức 12 triệu (đặc biệt)
TotalSalaryByHourOT12 = IIF(TotalSalary >= 12000000, 
    ROUND(12000000 / @NumberOfPayroll / 8, 2),  -- Nếu lương >= 12M, chỉ tính OT trên 12M
    0                                            -- Nếu lương < 12M, không áp dụng
)
```

#### 📊 Các Trường Tính Toán

```sql
-- Từ HR_tblViewCheckINOut (Chấm công)
NumberWorking           -- Số ngày làm việc
TotalTime               -- Tổng giờ làm (normal)
OT                      -- Giờ tăng ca (overtime 150%)
OTSun                   -- Giờ làm chủ nhật (200%)
OTNight                 -- Giờ làm đêm (extra 30%)
OTHoliday               -- Giờ làm ngày lễ (300%)
Holiday                 -- Giờ được hưởng lễ
P                       -- Phép (paid leave)
PDX                     -- Phép dự xuất (approved leave)
B                       -- Bù (compensatory day)
KL                      -- Không lương (unpaid)
TS                      -- Thai sản (maternity)

-- Lương tương ứng
SalaryTotalTime         -- Lương giờ normal
SalaryOT                -- Lương OT (150%)
SalaryOTSun             -- Lương chủ nhật (200%)
SalaryOTNight           -- Lương đêm (+30%)
SalaryOTHoliday         -- Lương ngày lễ (300%)
SalaryHoliday           -- Lương nghỉ lễ có hưởng
SalaryP                 -- Lương phép
SalaryPDX               -- Lương phép dự xuất

-- Thu nhập khác
SalaryTNKhac            -- Thu nhập khác (từ HR_tblOtherIncome)
SalaryDiligent          -- Tiền chuyên cần
SalaryChildPolicy       -- Phụ cấp con nhỏ (6-36 tháng tuổi)

-- Trừ khác
SalaryOthers            -- Trừ khác (từ HR_tblPayrollMinusOther)
SalaryBHXH              -- Bảo hiểm XH (employee contribution)
SalaryKPCD              -- Kinh phí công đoàn
TNCN                    -- Thuế TNCN (từ SS chuyển sang)

-- Tiền cơm
SalaryLunch             -- Cơm trưa (Group=0)
SalaryOTLunch           -- Cơm tăng ca (Group=1)
SalarySunLunch          -- Cơm chủ nhật (Group=2,4)
SalaryHolidayLunch      -- Cơm ngày lễ (Group=3)
```

#### 💰 Công Thức Tổng Lương

```sql
-- Tổng lương brutto (trước trừ)
SalaryFinal = SalaryTotalTime 
            + SalaryTotalTime85          -- Phần tăng từ EffectiveDate
            + SalaryOT + SalaryOT85
            + SalaryOTNight
            + SalaryOTSun + SalaryOTSun85
            + SalaryOTHoliday
            + SalaryHoliday + SalaryHoliday85
            + SalaryP + SalaryP85
            + SalaryPDX + SalaryPDX85
            + SalaryDiligent             -- Chuyên cần
            + SalaryTNKhac               -- Thu nhập khác
            + SalaryLunch + SalaryOTLunch + SalarySunLunch + SalaryHolidayLunch
            + SalaryCD                   -- Công đoàn nếu áp dụng
            + SalaryChildPolicy          -- Phụ cấp con nhỏ

-- Tổng lương thực nhận (SalaryReal)
SalaryReal = SalaryFinal 
           - SalaryBHXH                  -- Trừ BHXH (8% lương cơ bản)
           - SalaryKPCD                  -- Trừ công đoàn (1% lương cơ bản, max)
           - SalaryOthers                -- Trừ khác
           - TNCN                        -- Trừ thuế TNCN
```

#### 🔑 Nghiệp Vụ Quan Trọng

1. **Chế độ con nhỏ**:
```sql
-- Con từ 6-36 tháng tuổi (tính đến ngày 18 hàng tháng)
-- Công thức xác định tháng:
IIF(DAY(Birthday) <= 18, 
    DATEDIFF(MONTH, Birthday, FORMAT(@TransactionDate,'yyyy-MM-18')),
    DATEDIFF(MONTH, DATEADD(MONTH, -1, Birthday), FORMAT(@TransactionDate,'yyyy-MM-18'))
) BETWEEN 6 AND 36

SalaryChildPolicy = SoCon * @PhuCapConho  -- Mỗi con nhỏ * phụ cấp (config)
```

2. **Thuế TNCN**:
```sql
-- Import từ bảng SS (tính trước)
TNCN = (từ HR_tblPayrollSS.TNCN)
TNChiuThue = (từ HR_tblPayrollSS.TNChiuThue)
NoTaxIncome = (từ HR_tblPayrollSS.NoTaxIncome)
```

3. **Cập nhật công và lương**:
```sql
EXEC dbo.spUpdateTimeTotal @Transaction = @TransactionDate
-- SP này tính chi tiết từng loại công (P, OT, OTSun, Holiday, etc.)
```

---

### 3️⃣ **spGetPayroll** - Lấy Bảng Lương Chi Tiết
**Vị trí**: Lines 15039-15165  
**Mục đích**: Lấy dữ liệu bảng lương chi tiết đã tính (NB)

#### 📋 SELECT Statement (Simplified)

```sql
SELECT 
    -- Thông tin NV
    b.FullName, a.EmpID, b.DateStartWork, b.DateEndWork,
    d.SiteNameVN, e.DepartmentNameVN, h.ProductionLineNameVN,
    
    -- Lương cơ bản
    a.TotalSalary, a.TotalSalary85, 
    a.TotalSalaryByHour, a.TotalSalaryByHour85,
    a.BasicSalary, a.BasicSalaryByHour,
    
    -- Công
    a.TotalTime, a.OT, a.OTSun, a.OTNight, a.OTHoliday, 
    a.Holiday, a.P, a.PDX, a.B, a.CD, a.KL, a.TS,
    
    -- Lương theo công
    a.SalaryTotalTime, a.SalaryOT, a.SalaryOTSun, 
    a.SalaryOTNight, a.SalaryOTHoliday, a.SalaryHoliday,
    a.SalaryP, a.SalaryPDX,
    
    -- Thu nhập khác
    a.SalaryDiligent, a.SalaryTNKhac, a.SalaryChildPolicy,
    a.SalaryLunch, a.SalaryOTLunch, a.SalarySunLunch, a.SalaryHolidayLunch,
    
    -- Trừ
    a.SalaryBHXH, a.SalaryKPCD, a.TNCN, a.SalaryOthers,
    
    -- Tổng
    SalaryFinal = (tất cả thu nhập cộng lại),
    SalaryReal = (SalaryFinal - các khoản trừ),
    
    -- Ngân hàng
    i.BankAccountNumber, i.BankName,
    
    -- EffectiveDate & 85% tracking
    a.EffectiveDate, 
    a.TotalTime85, a.OT85, a.OTSun85, a.Holiday85, a.P85, a.PDX85,
    a.SalaryTotalTime85, a.SalaryOT85, a.SalaryOTSun85, a.SalaryHoliday85
    
FROM dbo.HR_tblPayroll a
WHERE a.MonthYear = FORMAT(@TransactionDate, 'yyyyMM')
  AND (a.TotalTime + a.P + a.PDX + a.B + a.Holiday) > 0  -- Có làm việc
  AND a.TotalSalary > 0                                    -- Có lương
```

---

### 4️⃣ **spUpdatePayrollSSSecurity** - Tính Lương Sổ Sách (SS)
**Vị trí**: Lines 22751-22890  
**Mục đích**: Tính lương sổ sách (accounting payroll) - **QUAN TRỌNG NHẤT**

#### 🔐 Đặc Điểm: MÃ HÓA DỮ LIỆU (Encryption)

```sql
-- TẤT CẢ dữ liệu lương được mã hóa bằng ENCRYPTBYPASSPHRASE
-- Giải mã khi đọc: CAST(CAST(DecryptByPassPhrase(@Key, field) AS NVARCHAR(MAX)) AS FLOAT)
-- Mã hóa khi ghi: ENCRYPTBYPASSPHRASE(@Key, CAST(value AS NVARCHAR(MAX)))
```

#### 📊 Các Bước Tính Lương SS

```sql
-- BƯỚC 1: Giải mã dữ liệu benefit
SELECT 
    CAST(CAST(DecryptByPassPhrase(@Key, a.TotalSalary) AS NVARCHAR(MAX)) AS FLOAT) TotalSalary,
    CAST(CAST(DecryptByPassPhrase(@Key, a.TotalSalary85) AS NVARCHAR(MAX)) AS FLOAT) TotalSalary85,
    CAST(CAST(DecryptByPassPhrase(@Key, a.BasicSalary) AS NVARCHAR(MAX)) AS FLOAT) BasicSalary,
    -- ... các trường khác
INTO #PayrollBenefits
FROM dbo.HR_tblPayrollBenefitsInternal a

-- BƯỚC 2: Insert/Update bảng SS với mã hóa
INSERT INTO dbo.HR_tblPayrollSSSecurity(...)
SELECT 
    ENCRYPTBYPASSPHRASE(@Key, CAST(a.TotalSalary AS NVARCHAR(MAX))),
    ENCRYPTBYPASSPHRASE(@Key, CAST(a.TotalSalary85 AS NVARCHAR(MAX))),
    -- ...
FROM #PayrollBenefits a

-- BƯỚC 3: Tính công và lương (gọi SP khác)
EXEC dbo.spUpdateTotalSalarySSSecurity @TransactionDate = @TransactionDate, @Key = @Key

-- BƯỚC 4: Cập nhật chuyên cần (từ bảng Diligent)
UPDATE dbo.HR_tblPayrollSSSecurity 
SET SalaryDiligent = ENCRYPTBYPASSPHRASE(@Key, CAST(a.SalaryDiligent AS NVARCHAR(MAX))) 
FROM (SELECT EmpID, SalaryDiligent FROM dbo.HR_tblPayrollDiligentSecurity ...) a

-- BƯỚC 5: Cập nhật bảo hiểm
EXEC dbo.spUpdatePayrollSSSecurityFinal @TransactionDate = ..., @Key = @Key

-- BƯỚC 6: Tính thu nhập không chịu thuế (NoTaxIncome)
NoTaxIncome = SoNguoiPhuThuoc * @NguoiPhuThuoc    -- Mỗi người 4.4M
            + @GiamTruBanThan                      -- Bản thân 11M
            + SalaryBHXH                           -- Bảo hiểm được trừ

-- BƯỚC 7: Tính thu nhập chịu thuế (TNChiuThue)
TNChiuThue = SalaryTotalTime          -- Lương giờ normal
           + SalaryDiligent           -- Chuyên cần
           + SalaryHoliday            -- Lương nghỉ lễ
           + SalaryP                  -- Lương phép
           + SalaryPDX                -- Lương phép dự xuất
           - NoTaxIncome              -- Trừ giảm trừ

-- BƯỚC 8: Tính thuế TNCN (progressive tax)
SELECT 
    ISNULL(CAST(CAST(DecryptByPassPhrase(@Key, a.TNChiuThue) AS NVARCHAR(MAX)) AS FLOAT), 0) TNChiuThue,
    b.Tax,                             -- % thuế từ bảng PersionalIncomTax
    (TNChiuThue * b.Tax) / 100 - b.Money ThueTNCN,
    a.NoTax, 
    c.WorkingStatusID
INTO #ThueTNCN
FROM dbo.HR_tblPayrollSSSecurity a
LEFT JOIN dbo.PersionalIncomTax b 
    ON TNChiuThue BETWEEN b.[From] AND b.[To]

-- BƯỚC 9: Update TNCN (với rule đặc biệt cho nhân viên thử việc)
UPDATE dbo.HR_tblPayrollSSSecurity 
SET TNCN = ENCRYPTBYPASSPHRASE(@Key, CAST(
    IIF(a.NoTax = 0 AND a.WorkingStatusID = 0,   -- Thử việc KHÔNG có cam kết
        ROUND(a.SalaryTotal * 0.1, 0),           -- → Trừ 10% flat
        ROUND(a.ThueTNCN, 0)                     -- → Trừ theo lũy tiến
    ) AS NVARCHAR(MAX)))
FROM #ThueTNCN a
```

#### 💡 Ý Nghĩa Các Trường SS

```sql
-- Bảng: HR_tblPayrollSSSecurity
TotalSalary              -- Tổng lương (encrypted)
TotalSalary85            -- Lương 85% (encrypted)
TotalSalaryByHour        -- Lương/giờ (encrypted)
BasicSalary              -- Lương cơ bản (encrypted, để tính BHXH)
SalaryTotalTime          -- Lương giờ normal (encrypted)
SalaryDiligent           -- Chuyên cần (encrypted)
SalaryHoliday            -- Lương nghỉ lễ (encrypted)
SalaryP                  -- Lương phép (encrypted)
SalaryPDX                -- Lương phép dự xuất (encrypted)
NoTaxIncome              -- Thu nhập không chịu thuế (encrypted)
TNChiuThue               -- Thu nhập chịu thuế (encrypted)
TNCN                     -- Thuế TNCN (encrypted)
SalaryBHXH               -- BHXH NV đóng (encrypted)
SalaryKPCD               -- Công đoàn (encrypted)
SalaryBHXHCompany        -- BHXH công ty đóng (encrypted)
NumberWorking            -- Số ngày làm
NoTax                    -- Cờ cam kết không đóng thuế
EffectiveDate            -- Ngày hiệu lực lương mới
```

---

### 5️⃣ **spGetPayrollSS** - Lấy Bảng Lương Sổ Sách
**Vị trí**: Lines 15753-15820  
**Mục đích**: Lấy dữ liệu bảng lương sổ sách đã tính

#### 📋 SELECT (Simplified)

```sql
SELECT 
    -- Thông tin NV
    b.FullName, a.EmpID, b.DateStartWork, b.DateEndWork,
    
    -- Lương cơ bản
    a.TotalSalary, a.TotalSalaryByHour, a.BasicSalary,
    
    -- Công
    a.TotalTime, a.OT, a.OTSun, a.OTHoliday, a.Holiday,
    ISNULL(a.P, 0) + ISNULL(a.PDX, 0) TotalP,    -- Tổng phép
    
    -- Lương
    a.SalaryTotalTime, a.SalaryOT, a.SalaryOTSun, a.SalaryHoliday,
    ISNULL(a.SalaryP, 0) + ISNULL(a.SalaryPDX, 0) SalaryP,
    a.SalaryDiligent,
    
    -- Thuế & BH
    a.NoTaxIncome, a.TNChiuThue, a.TNCN,
    a.SalaryBHXH, a.SalaryKPCD,
    a.SalaryBHXHCompany,                         -- Công ty đóng
    a.SalaryBHXHCompany + a.SalaryBHXH TotalBHXH,-- Tổng BH
    
    -- Tổng lương
    SalaryFinal = a.SalaryTotalTime 
                + a.SalaryOT 
                + a.SalaryOTSun 
                + a.SalaryHoliday 
                + a.SalaryP 
                + a.SalaryPDX 
                + a.SalaryDiligent
                + a.SalaryCD,
    
    SalaryReal = SalaryFinal 
               - a.SalaryBHXH 
               - a.SalaryKPCD 
               - a.SalaryOthers 
               - a.TNCN
               
FROM dbo.HR_tblPayrollSSSecurity a
WHERE a.MonthYear = FORMAT(@TransactionDate, 'yyyyMM')
  AND (a.TotalTime + a.P + a.PDX + a.B + a.Holiday) > 0
```

---

### 6️⃣ **spUpdatePayrollDiligentSecurity** - Tính Chuyên Cần
**Vị trí**: Lines 21894-22068  
**Mục đích**: Tính tiền thưởng chuyên cần (diligent bonus)

#### 🎯 Logic Tính Chuyên Cần

```sql
-- Các thông số
@Diligent = 1000000         -- Tiền thưởng chuyên cần tối đa (config)
@Forgot = 50000             -- Phạt quên chấm công mỗi lần (config)

-- Đếm vi phạm
CountLate                   -- Số lần đi muộn
CountKL                     -- Số ngày không lương (KL/8)
CountB                      -- Số lần bù
CountPDX                    -- Số ngày phép dự xuất (PDX/8)
CountForgot                 -- Số lần quên chấm công
CountTS                     -- Số lần thai sản

-- CÔNG THỨC TÍNH:
SalaryDiligent = CASE
    -- Loại trừ: Nghỉ việc, mới vào, TS >= 3, chuyền đặc biệt, đi công tác KCC
    WHEN (DateEndWork BETWEEN @FromDate AND @ToDate)    -- Nghỉ việc trong tháng
         OR DateStartWork > @FromDate                   -- Mới vào giữa tháng
         OR CountTS >= 3                                -- Thai sản >= 3 ngày
         OR ProductionLineID = 'PL048'                  -- Chuyền đặc biệt
         OR EXISTS (công tác KCC trong tháng)
    THEN 0
    
    -- Có flag IsDiligent=1 (đảm bảo luôn có chuyên cần)
    WHEN IsDiligent = 1 
    THEN @Diligent                                      -- → 1,000,000 đầy đủ
    
    -- Không vi phạm gì + quên chấm công
    WHEN (CountB + CountKL + CountLate + CountPDX) = 0 AND CountTS = 0
    THEN @Diligent - (@Forgot * CountForgot)            -- → 1M - (50k * số lần quên)
    
    -- Vi phạm ít: B+KL+PDX <= 1, đi muộn <= 2
    WHEN (CountB + CountKL + CountPDX) <= 1 AND CountLate <= 2
    THEN 500000 - (@Forgot * CountForgot)               -- → 500k - phạt quên
    
    -- Vi phạm vừa: B+KL+PDX <= 2, đi muộn <= 3
    WHEN (CountB + CountKL + CountPDX) <= 2 AND CountLate <= 3
    THEN 300000 - (@Forgot * CountForgot)               -- → 300k - phạt quên
    
    -- Vi phạm nhiều
    ELSE 0                                               -- → Không có chuyên cần
END
```

#### 📋 Các Vi Phạm & Điều Kiện

```sql
-- Đếm vi phạm từ HR_tblViewCheckINOut
SELECT 
    a.EmpID,
    COUNT(CASE WHEN a.WorkerLate = 1 AND a.IsHolyday = 0 THEN 1 END) CountLate,
    SUM(a.KL) / 8.0 CountKL,                -- KL tính theo ngày
    COUNT(CASE WHEN a.B > 0 THEN 1 END) CountB,
    SUM(a.PDX) / 8.0 CountPDX,              -- PDX tính theo ngày
    COUNT(CASE WHEN a.ForgotCheckINOUT > 0 THEN 1 END) CountForgot,
    COUNT(CASE WHEN a.TS > 0 THEN 1 END) CountTS
FROM dbo.HR_tblViewCheckINOut a
WHERE a.CheckInOutDate BETWEEN @FromDate AND @ToDate
  AND NOT EXISTS (SELECT 1 FROM HR_tblHolidaysDuringYear WHERE HolidayType='KL' AND BeginHolidayDate = a.CheckInOutDate)
GROUP BY a.EmpID
```

#### 🔐 Cập Nhật Bảo Hiểm (Encrypted)

```sql
-- Lấy % thuế từ HR_TblPerTax
@BHTN = 1%              -- Bảo hiểm thất nghiệp (employee)
@BHXH = 8%              -- Bảo hiểm xã hội (employee)
@BHYT = 1.5%            -- Bảo hiểm y tế (employee)
@KPCD = 1%              -- Kinh phí công đoàn (max @MaxCongDoan)
@BHXH_NSD = 17.5%       -- BHXH công ty đóng (company)
@BHTNComp = 1%          -- BHTN công ty
@BHXHComp = 17%         -- BHXH công ty
@BHYTComp = 3%          -- BHYT công ty
@KPCDComp = 2%          -- KPCD công ty
@TNLD = 0.5%            -- Tai nạn lao động

-- Tính
BHXH = BasicSalary * @BHXH
BHYT = BasicSalary * @BHYT
BHTN = BasicSalary * @BHTN
SalaryKPCD = IIF(BasicSalary * @KPCD > @MaxCongDoan, @MaxCongDoan, BasicSalary * @KPCD)
SalaryBHXH_NSD = BasicSalary * @BHXH_NSD   -- Công ty đóng
BHXHCompany = BasicSalary * @BHXHComp
BHYTCompany = BasicSalary * @BHYTComp
BHTNCompany = BasicSalary * @BHTNComp
KPCDCompany = BasicSalary * @KPCDComp
TNLD = BasicSalary * @TNLD

-- Mã hóa khi lưu
UPDATE HR_tblPayrollDiligentSecurity 
SET BHXH = ENCRYPTBYPASSPHRASE(@Key, CAST(a.BHXH AS NVARCHAR(MAX))),
    BHYT = ENCRYPTBYPASSPHRASE(@Key, CAST(a.BHYT AS NVARCHAR(MAX))),
    -- ... tương tự
```

---

### 7️⃣ **spUpdatePayrollNBSecrity** - Tính Lương NB (Encrypted)
**Vị trí**: Lines 22285-22404  
**Mục đích**: Tính lương chi tiết NB với mã hóa

#### 🔢 Công Thức (với Encryption)

```sql
-- 1. Tính lương/giờ (encrypted)
TotalSalaryByHour = ENCRYPTBYPASSPHRASE(@Key, CAST(
    IIF(IsSecurity = 1,
        ROUND(CAST(DECRYPTBYPASSPHRASE(@Key, TotalSalaryNB) AS FLOAT) / @Day / 8, 2),
        ROUND(CAST(DECRYPTBYPASSPHRASE(@Key, TotalSalaryNB) AS FLOAT) / @NumberOfPayroll / 8, 2)
    ) AS NVARCHAR(MAX)))

-- 2. Tính lương OT mức 12M
TotalSalaryByHourOT12 = ENCRYPTBYPASSPHRASE(@Key, CAST(
    IIF(CAST(DECRYPTBYPASSPHRASE(@Key, TotalSalaryNB) AS FLOAT) >= 12000000,
        ROUND(12000000 / @NumberOfPayroll / 8, 2),
        0
    ) AS NVARCHAR(MAX)))

-- 3. Cập nhật công và lương (SP khác với encryption)
EXEC dbo.spUpdateTimeTotalSecrity @Transaction = @TransactionDate, @Key = @Key

-- 4. Thu nhập khác (gồm phụ cấp y tế theo công làm)
SalaryTNKhac = SUM(
    HR_tblOtherIncome.Total +
    (HealthCareSupport / @NumberOfPayroll / 8) * (TotalTime + OTSun)
)

-- 5. Tiền cơm từ HR_tblAllowance
SalaryLunch         = ENCRYPTBYPASSPHRASE(@Key, ...) WHERE [Group] = 0
SalaryOTLunch       = ENCRYPTBYPASSPHRASE(@Key, ...) WHERE [Group] = 1
SalarySunLunch      = ENCRYPTBYPASSPHRASE(@Key, ...) WHERE [Group] IN (2,4)
SalaryHolidayLunch  = ENCRYPTBYPASSPHRASE(@Key, ...) WHERE [Group] = 3

-- 6. Chế độ con nhỏ (như trên)
SalaryChildPolicy = ENCRYPTBYPASSPHRASE(@Key, CAST((SoCon * @PhuCapConho) AS NVARCHAR(MAX)))

-- 7. Import thuế từ SS
TNCN = (từ HR_tblPayrollSSSecurity.TNCN)
TNChiuThue = (từ HR_tblPayrollSSSecurity.TNChiuThue)
NoTaxIncome = (từ HR_tblPayrollSSSecurity.NoTaxIncome)
```

---

## 📚 BẢNG THAM CHIẾU NHANH

### Config Values (HR_tblConfig)

| Id | Ý Nghĩa | Giá Trị Mẫu |
|----|---------|-------------|
| 3 | Thưởng chuyên cần | 1,000,000 |
| 4 | Giảm trừ bản thân | 11,000,000 |
| 5 | Giảm trừ người phụ thuộc | 4,400,000 |
| 9 | Phạt quên chấm công | 50,000 |
| 11 | Phụ cấp con nhỏ | 200,000 |
| 12 | Max công đoàn | 210,000 |

### Bảng Thuế TNCN Lũy Tiến (PersionalIncomTax)

| Từ (From) | Đến (To) | Thuế (Tax) | Trừ (Money) |
|-----------|----------|------------|-------------|
| 0 | 5,000,000 | 5% | 0 |
| 5,000,001 | 10,000,000 | 10% | 250,000 |
| 10,000,001 | 18,000,000 | 15% | 750,000 |
| 18,000,001 | 32,000,000 | 20% | 1,650,000 |
| 32,000,001 | 52,000,000 | 25% | 3,250,000 |
| 52,000,001 | 80,000,000 | 30% | 5,850,000 |
| 80,000,001 | 999,999,999 | 35% | 9,850,000 |

### Bảng % Bảo Hiểm (HR_TblPerTax)

| Loại | TaxCodeType | Employee (%) | Company (%) |
|------|-------------|--------------|-------------|
| BHXH | BHXH | 8% | 17% |
| BHYT | BHYT | 1.5% | 3% |
| BHTN | BHTN | 1% | 1% |
| Công đoàn | KPCĐ | 1% (max 210k) | 2% |
| TNLĐ | TNLĐ | 0% | 0.5% |
| **Tổng** | All | 10.5% | **17.5%** |

---

## 🔄 WORKFLOW TÍNH LƯƠNG

### Quy Trình Tổng Thể

```
1. Maintain PayrollBenefits (Lương cơ bản)
   ↓
2. Chấm công (HR_tblViewCheckINOut)
   ↓
3. Tính chuyên cần (spUpdatePayrollDiligentSecurity)
   ↓
4. Tính SS - Sổ sách (spUpdatePayrollSSSecurity)
   ├─ Giải mã benefit
   ├─ Tính công & lương
   ├─ Cập nhật chuyên cần
   ├─ Cập nhật bảo hiểm
   ├─ Tính TNChiuThue
   └─ Tính TNCN (thuế)
   ↓
5. Tính NB - Chi tiết (spUpdatePayrollNBSecrity hoặc spUpdatePayroll)
   ├─ Giải mã benefit
   ├─ Tính công & lương chi tiết
   ├─ Thu nhập khác
   ├─ Tiền cơm
   ├─ Chế độ con nhỏ
   └─ Import thuế từ SS
   ↓
6. Query bảng lương (spGetPayrollSS / spGetPayroll)
```

### Chi Tiết Từng Bước

#### Bước 1: Maintain PayrollBenefits
```sql
-- Thêm/Sửa lương cơ bản
EXEC spInsertPayrollBenefits @EmpID, @TotalSalary, @BasicSalary, @EffectiveDate, ...
EXEC spUpdatePayrollBenefits @EmpID, @TotalSalary, @BasicSalary, @EffectiveDate, ...
```

#### Bước 2: Chấm Công
```sql
-- Dữ liệu từ HR_tblViewCheckINOut (View tổng hợp)
-- Chứa: TimeIn, TimeOut, TotalTime, OT, OTSun, P, PDX, B, KL, TS, etc.
```

#### Bước 3: Tính Chuyên Cần
```sql
EXEC spUpdatePayrollDiligentSecurity 
    @TransactionDate = '2024-01-31',
    @CreateUser = '03794',
    @Key = 'your-encryption-key'
```

#### Bước 4: Tính SS (Sổ Sách)
```sql
EXEC spUpdatePayrollSSSecurity 
    @TransactionDate = '2024-01-31',
    @CreateUser = '03794',
    @Key = 'your-encryption-key'
```

#### Bước 5: Tính NB (Chi Tiết)
```sql
-- Non-encrypted version
EXEC spUpdatePayroll 
    @TransactionDate = '2024-01-31',
    @FromDate = '2024-01-01',
    @ToDate = '2024-01-31',
    @CreateUser = '03794'

-- Encrypted version
EXEC spUpdatePayrollNBSecrity 
    @TransactionDate = '2024-01-31',
    @FromDate = '2024-01-01',
    @ToDate = '2024-01-31',
    @CreateUser = '03794',
    @Key = 'your-encryption-key'
```

#### Bước 6: Lấy Kết Quả
```sql
-- Lấy bảng lương SS
EXEC spGetPayrollSS 
    @Option = 1,
    @TransactionDate = '2024-01-31',
    @EmpID = NULL  -- NULL = tất cả NV

-- Lấy bảng lương NB
EXEC spGetPayroll 
    @Option = 1,
    @TransactionDate = '2024-01-31',
    @EmpID = NULL
```

---

## 🔑 CÁC ĐIỂM QUAN TRỌNG CẦN NHỚ

### 1. EffectiveDate & 85% Rule
```sql
-- Nguyên tắc:
-- Nếu EffectiveDate >= (TotalDays * 0.85) → Dùng lương mới toàn bộ
-- Nếu không → Tính pro-rata (split theo ngày)

-- Ví dụ tháng 30 ngày:
-- EffectiveDate = 26 → 26/30 = 86.67% >= 85% → Lương mới toàn bộ
-- EffectiveDate = 25 → 25/30 = 83.33% < 85% → Split

-- Công thức split:
SalaryPart1 = (OldSalary / TotalDays) * DaysBeforeEffective
SalaryPart2 = (NewSalary / TotalDays) * DaysAfterEffective
TotalSalary = SalaryPart1 + SalaryPart2
```

### 2. SS vs NB
```sql
-- SS (Sổ Sách): 
--   - Báo cáo kế toán
--   - Danh mục: P, OT, OTSun, Holiday
--   - Ít chi tiết hơn
--   - Tính thuế TNCN tại đây

-- NB (Chi Tiết):
--   - Bảng lương chi tiết cho NV
--   - Danh mục: TotalTime, P, OT, OTSun, OTNight, OTHoliday, Holiday, PDX, B, KL, TS, CD
--   - Chi tiết hơn
--   - Import thuế từ SS
```

### 3. Encryption Pattern
```sql
-- Giải mã:
CAST(CAST(DecryptByPassPhrase(@Key, EncryptedField) AS NVARCHAR(MAX)) AS FLOAT)

-- Mã hóa:
ENCRYPTBYPASSPHRASE(@Key, CAST(Value AS NVARCHAR(MAX)))

-- Tất cả dữ liệu lương trong:
--   - HR_tblPayrollBenefitsInternal
--   - HR_tblPayrollSSSecurity
--   - HR_tblPayrollNBSecurity
--   - HR_tblPayrollDiligentSecurity
-- đều được mã hóa
```

### 4. Thuế TNCN
```sql
-- Công thức:
TNChiuThue = SalaryTotalTime 
           + SalaryDiligent 
           + SalaryHoliday 
           + SalaryP 
           + SalaryPDX
           - NoTaxIncome

-- NoTaxIncome:
NoTaxIncome = @GiamTruBanThan        -- 11M
            + (SoNguoiPhuThuoc * @NguoiPhuThuoc)  -- 4.4M/người
            + SalaryBHXH             -- BHXH được trừ

-- Thuế:
TNCN = CASE
    WHEN NoTax = 0 AND WorkingStatusID = 0 THEN SalaryTotal * 0.1  -- Thử việc không cam kết
    ELSE (TNChiuThue * Tax%) - Money                               -- Lũy tiến
END
```

### 5. Chuyên Cần (Diligent)
```sql
-- Công thức:
SalaryDiligent = CASE
    WHEN (vi phạm loại trừ) THEN 0
    WHEN IsDiligent = 1 THEN 1,000,000
    WHEN (không vi phạm) THEN 1,000,000 - (50,000 * CountForgot)
    WHEN (vi phạm ít) THEN 500,000 - (50,000 * CountForgot)
    WHEN (vi phạm vừa) THEN 300,000 - (50,000 * CountForgot)
    ELSE 0
END

-- Vi phạm loại trừ:
-- - Nghỉ việc trong tháng
-- - Mới vào giữa tháng
-- - Thai sản >= 3 ngày
-- - Chuyền đặc biệt (PL048)
-- - Đi công tác KCC
```

### 6. Bảo Hiểm
```sql
-- Employee contribution (trừ vào lương):
SalaryBHXH = BasicSalary * 8%        -- BHXH
BHYT = BasicSalary * 1.5%            -- BHYT
BHTN = BasicSalary * 1%              -- BHTN
SalaryKPCD = MIN(BasicSalary * 1%, 210,000)  -- Công đoàn (max 210k)

-- Company contribution (không trừ lương NV):
SalaryBHXH_NSD = BasicSalary * 17.5%  -- Tổng công ty đóng
BHXHCompany = BasicSalary * 17%
BHYTCompany = BasicSalary * 3%
BHTNCompany = BasicSalary * 1%
KPCDCompany = BasicSalary * 2%
TNLD = BasicSalary * 0.5%
```

### 7. Chế Độ Con Nhỏ
```sql
-- Điều kiện:
-- - Con từ 6-36 tháng tuổi
-- - Tính đến ngày 18 hàng tháng
-- - Mỗi con: 200,000đ (config)

-- Công thức xác định tháng tuổi:
IIF(DAY(Birthday) <= 18,
    DATEDIFF(MONTH, Birthday, FORMAT(@TransactionDate, 'yyyy-MM-18')),
    DATEDIFF(MONTH, DATEADD(MONTH, -1, Birthday), FORMAT(@TransactionDate, 'yyyy-MM-18'))
) BETWEEN 6 AND 36

SalaryChildPolicy = SoCon * @PhuCapConho
```

---

## 📝 VÍ DỤ TÍNH LƯƠNG CỤ THỂ

### Ví Dụ 1: Nhân Viên Bình Thường

```sql
-- Thông tin NV:
EmpID = 'EMP001'
TotalSalary = 10,000,000
BasicSalary = 5,000,000
NumberOfPayroll = 26

-- Công trong tháng:
TotalTime = 208h (26 ngày * 8h)
OT = 20h
P = 8h (1 ngày phép)

-- Bước 1: Tính lương/giờ
TotalSalaryByHour = 10,000,000 / 26 / 8 = 48,076.92đ

-- Bước 2: Tính lương công
SalaryTotalTime = 208h * 48,076.92 = 10,000,000đ
SalaryOT = 20h * 48,076.92 * 1.5 = 1,442,307đ
SalaryP = 8h * 48,076.92 = 384,615đ

-- Bước 3: Chuyên cần (không vi phạm, quên 1 lần)
SalaryDiligent = 1,000,000 - (50,000 * 1) = 950,000đ

-- Bước 4: Bảo hiểm
SalaryBHXH = 5,000,000 * 10.5% = 525,000đ

-- Bước 5: Tính thuế
NoTaxIncome = 11,000,000 + 525,000 = 11,525,000đ
TNChiuThue = 10,000,000 + 384,615 + 950,000 - 11,525,000 = -190,385đ → 0
TNCN = 0đ (vì TNChiuThue = 0)

-- Tổng lương:
SalaryFinal = 10,000,000 + 1,442,307 + 384,615 + 950,000 = 12,776,922đ
SalaryReal = 12,776,922 - 525,000 - 0 = 12,251,922đ
```

### Ví Dụ 2: NV Thử Việc Không Cam Kết

```sql
-- Thông tin:
WorkingStatusID = 0 (Thử việc)
NoTax = 0 (Không cam kết)
TotalSalary = 8,000,000
SalaryFinal = 8,500,000 (sau cộng OT, chuyên cần, etc.)

-- Thuế TNCN:
-- Không tính lũy tiến, chỉ trừ 10% flat
TNCN = 8,500,000 * 0.1 = 850,000đ

SalaryReal = 8,500,000 - BHXH - 850,000
```

### Ví Dụ 3: Tăng Lương Giữa Tháng (EffectiveDate)

```sql
-- Thông tin:
OldSalary = 8,000,000
NewSalary = 10,000,000
EffectiveDate = 2024-01-20
FromDate = 2024-01-01
ToDate = 2024-01-31
TotalDays = 31

-- Tính 85% rule:
DaysBeforeEffective = 20 - 1 = 19
PercentEffective = 20 / 31 = 64.52% < 85% → SPLIT

-- Tính pro-rata:
SalaryPart1 = (8,000,000 / 31) * 19 = 4,903,225đ
SalaryPart2 = (10,000,000 / 31) * 12 = 3,870,967đ
TotalSalary = 4,903,225 + 3,870,967 = 8,774,192đ

-- Lưu cả 2 phần:
TotalSalary = 8,774,192đ       -- Tổng
TotalSalary85 = 3,870,967đ     -- Phần tăng từ EffectiveDate
```

---

## 🎓 HƯỚNG DẪN HỌC

### Bước 1: Hiểu Cấu Trúc Bảng
1. **HR_tblPayrollBenefits**: Lương cơ bản (master data)
2. **HR_tblPayrollSS**: Kết quả tính lương sổ sách
3. **HR_tblPayroll**: Kết quả tính lương chi tiết
4. **HR_tblPayrollDiligent**: Kết quả tính chuyên cần
5. **HR_tblViewCheckINOut**: View chấm công tổng hợp

### Bước 2: Hiểu Workflow
1. Maintain benefit
2. Chấm công
3. Tính chuyên cần
4. Tính SS (có thuế)
5. Tính NB (import thuế từ SS)

### Bước 3: Học Công Thức
1. Lương/giờ (Hour rate)
2. TNChiuThue (Taxable income)
3. TNCN (Personal tax)
4. Chuyên cần (Diligent bonus)
5. Bảo hiểm (Insurance)
6. EffectiveDate split

### Bước 4: Thực Hành
1. Chạy SP với data mẫu
2. Verify kết quả từng bước
3. So sánh với ví dụ

---

## 📖 TÀI LIỆU LIÊN QUAN

Tham khảo thêm các tài liệu trong thư mục `docs/`:
1. **Dev_Guide_Payroll_Tính_Lương_Chi_Tiết.md**: Hướng dẫn code ABP 9
2. **Phan_Tich_Chi_Tiet_Tinh_Luong_TiengViet.md**: Phân tích chi tiết công thức
3. **Quick_Reference_Tính_Lương.md**: Cheat sheet tra cứu nhanh
4. **Common_Scenarios_Troubleshooting.md**: Xử lý các trường hợp đặc biệt

---

**Cập nhật**: 15/12/2025  
**Version**: 1.0  
**Tác giả**: BA + TechLead Analysis
Copilot said: Cảm ơn bạn đã chia sẻ tài
Cảm ơn bạn đã chia sẻ tài liệu học Stored Procedures tính lương chi tiết. Nội dung rất đầy đủ, có cấu trúc rõ ràng theo SP chính, bảng/tham số, workflow, và ví dụ minh họa. Nếu mục tiêu là để học và triển khai/kiểm thử, dưới đây là một số gợi ý giúp bạn tận dụng tài liệu hiệu quả:

Kiểm thử theo workflow chuẩn:

Insert/Update PayrollBenefits với các case: tăng lương giữa tháng, có/không 85% rule, thử việc có/không cam kết thuế.
Chuẩn bị dữ liệu chấm công tổng hợp (HR_tblViewCheckINOut) cho các loại công: OT 150%, OTSun 200%, OTHoliday 300%, OTNight +30%, P, PDX, B, KL, TS.
Chạy 
 với @Key để tính chuyên cần và bảo hiểm (employee + company).
Chạy spUpdatePayrollSSSecurity để tính SS, TNChiuThue và TNCN (lũy tiến/10%).
Chạy spUpdatePayroll hoặc spUpdatePayrollNBSecrity để tính NB chi tiết và import thuế từ SS.
Dùng spGetPayrollSS và spGetPayroll để đối soát tổng và thực nhận.
Bộ test case khuyến nghị:

Tăng lương EffectiveDate ở ba mốc: 25/30 (<85%), 26/30 (≥85%), đầu/giữa/cuối tháng 28-31 ngày.
NV thử việc: WorkingStatusID=0, NoTax=0 vs NoTax=1.
Lương ≥12M để kiểm tra “OT capped at 12M” (TotalSalaryByHourOT12).
Chuyên cần: không vi phạm, vi phạm ít/vừa/nhiều, có quên chấm công 0/1/n lần; loại trừ (nghỉ việc trong tháng, mới vào, TS≥3, PL048, công tác KCC).
Con nhỏ: biên 6, 36 tháng (ngày sinh trước/sau ngày 18).
Công đoàn: kiểm tra trần 210k.
Các điểm cần chú ý khi triển khai:

Encryption/Decryption: đảm bảo dùng đúng kiểu CAST NVARCHAR(MAX) → FLOAT khi giải mã; viết unit test cho các cột tiền để tránh sai số làm tròn.
85% Rule: thống nhất cách tính “ngày trong tháng” (28/29/30/31) và cách lấy DaysBeforeEffective vs DaysAfterEffective (có tính inclusive/exclusive ngày hiệu lực).
TNCN: đảm bảo mapping PersionalIncomTax đúng khoảng [From, To], và xử lý TNChiuThue < 0 → 0.
KPCĐ: áp dụng min(BasicSalary*1%, MaxCongDoan) ở cả NB/SS nơi cần.
Import thuế từ SS sang NB: kiểm tra transaction order để NB luôn lấy giá trị mới nhất từ SS cùng kỳ.
Đề xuất cải tiến tài liệu:

Thêm pseudo-SQL cho cách tính TotalSalary85 và TotalTime85/OT85/P85/Holiday85 để người đọc thấy rõ cách split theo EffectiveDate.
Liệt kê đầy đủ các bảng nội bộ mã hóa: HR_tblPayrollBenefitsInternal vs HR_tblPayrollBenefits (nếu có bản không mã hóa), và mối quan hệ giữa các bảng Security/NB/SS.
Bổ sung ví dụ về OTNight (+30%) và OTHoliday (300%) vào phần ví dụ tính lương.
Thêm checklist xác thực dữ liệu: khớp tổng công giờ theo ngày, loại trừ giờ lễ khỏi bị tính late, và xử lý edge cases ForgotCheckINOUT.
Nếu bạn muốn, mình có thể:

Tạo bộ script mẫu để chạy tuần tự toàn bộ workflow với dữ liệu giả lập.
Viết unit test SQL (tạm bằng các SELECT/ASSERT) cho các công thức nhạy cảm (85% rule, chuyên cần, thuế).
Chuyển tài liệu này thành một file Markdown tiêu chuẩn trong repo docs với các phần mục lục và liên kết chéo đến SP tương ứng.
Bạn muốn mình bắt đầu với bộ test case hay tạo file docs markdown chuẩn để commit vào repo?

You said: # 📘 Hướng Dẫn Tính Lương Chi
# 📘 Hướng Dẫn Tính Lương Chi Tiết Cho Developer
## ABP 9 + Công Thức Tính Lương + Best Practices

**Dành cho**: Developer  
**Scope**: Tính lương SS, NB, công thức, implementation, testing  
**Ngôn ngữ**: Tiếng Việt + Code (C#)  
**Ngày cập nhật**: 10/12/2025

---

## 📑 Mục Lục
1. Tổng Quan Hệ Thống
2. Kiến Trúc Dữ Liệu & Entities
3. Luồng Tính Lương Chi Tiết (SS)
4. Luồng Tính Lương NB
5. FormulaConfig & Dynamic Computation
6. Implementation Pattern (ABP 9)
7. Domain Services & Business Logic
8. Testing Strategy
9. Common Mistakes & Pitfalls

---

## 1️⃣ Tổng Quan Hệ Thống

### 1.1 Mục Đích Hai Loại Bảng Lương

```
┌─────────────────────────────────────────────────────────────────┐
│                    HỆ THỐNG TÍNH LƯƠNG                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌──────────────────────┐      ┌──────────────────────┐          │
│  │   SS (Sổ Sách)       │      │   NB (Chi Tiết)      │          │
│  ├──────────────────────┤      ├──────────────────────┤          │
│  │ • Tổng lương         │      │ • Phân rã theo danh  │          │
│  │ • Tính thuế TNCN     │      │   mục                │          │
│  │ • Lương BHXH         │      │ • Theo giờ/danh mục  │          │
│  │ • Báo cáo thống kê   │      │ • Chi tiết thanh toán│          │
│  │                      │      │ • Tính TNCN từ SS    │          │
│  └──────────────────────┘      └──────────────────────┘          │
│           │                              │                       │
│           └──────────────┬───────────────┘                       │
│                          │                                       │
│                   ┌──────▼──────┐                                │
│                   │  Net Salary  │  (Lương ròng)                 │
│                   └─────────────┘                                │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

**Quan hệ**: NB lấy TNCN từ SS → Tính Net

---

## 2️⃣ Kiến Trúc Dữ Liệu & Entities

### 2.1 Core Entities trong ABP 9

#### PayrollBenefit (Lương Cơ Sở)
```csharp
public class PayrollBenefit : BaseEntity
{
    // Thông tin cơ bản
    public PayrollBenefitTypeEnum Type { get; set; }  // Standard / Confidential
    public string EmployeeCode { get; set; }
    public string ContractTypeCode { get; set; }
    public SalaryPolicyTypeEnum SalaryPolicyCode { get; set; }
    
    // Lương (mã hóa nếu Confidential)
    public decimal TotalSalary { get; set; }           // Tổng lương
    public decimal TotalSalaryNB { get; set; }         // Lương dùng cho NB
    public decimal TotalSalary85 { get; set; }         // Lương sau ngày hiệu lực
    public decimal BasicSalary { get; set; }           // Lương cơ sở
    public decimal TotalSalaryByHour { get; set; }     // Lương/giờ
    
    // Phụ cấp
    public decimal TravelSupportMoney { get; set; }    // Phụ cấp đi lại
    public decimal PhoneSupportMoney { get; set; }     // Phụ cấp điện thoại
    public decimal HousingSupportMoney { get; set; }   // Phụ cấp nhà
    public decimal HealthCareSupport { get; set; }     // Phụ cấp sức khỏe
    // ... các phụ cấp khác
    
    // Ngày hiệu lực
    public DateTime? EffectiveDate { get; set; }       // Ngày thay đổi lương
    
    // Các cột mã hóa (cho Confidential Type)
    public string TotalSalaryEncrypted { get; set; }
    public string BasicSalaryEncrypted { get; set; }
    // ... các trường nhạy cảm khác
}
```

**Mô tả:**
- Dùng để lưu lương cơ bản của mỗi nhân viên
- `Type = Confidential`: Dùng mã hóa cho các trường lương
- `EffectiveDate`: Xử lý tăng/giảm lương trong kỳ (phân tách 85%)
- Encrypted fields: Mã hóa asymmetric cho dữ liệu nhạy cảm

#### PayrollSS (Sổ Sách)
```csharp
public class PayrollSS : BaseEntity
{
    // Chỉ định
    public string EmployeeCode { get; set; }
    public string MonthYear { get; set; }              // YYYY-MM
    public string BeginPayrollDate { get; set; }
    public string EndingPayrollDate { get; set; }
    
    // Lương tính toán
    public decimal? TotalSalary { get; set; }          // Tổng lương (cơ bản + phụ cấp)
    public decimal? TotalSalary85 { get; set; }        // Lương 85% (sau EffectiveDate)
    public decimal? BasicSalary { get; set; }          // Lương cơ sở
    public decimal? TotalSalaryByHour { get; set; }    // Lương/giờ
    
    // Các khoản thu nhập khác
    public decimal? TravelSupportMoney { get; set; }
    public decimal? HealthCareSupport { get; set; }
    public decimal? OtherIncome { get; set; }
    
    // Thuế & bảo hiểm
    public decimal? TNChiuThue { get; set; }           // Thu nhập chịu thuế
    public decimal? TNCN { get; set; }                 // Thuế cá nhân
    public decimal? SalaryBHXH { get; set; }           // Lương đóng BHXH
    public decimal? SalaryBHXHCompany { get; set; }    // BHXH phần công ty
    public decimal? SalaryKPCDCompany { get; set; }    // KPCD phần công ty
    
    // Chấm công (danh mục)
    public decimal? P { get; set; }                    // Ngày công bình thường
    public decimal? P85 { get; set; }                  // P sau ngày hiệu lực
    public decimal? OT { get; set; }                   // Giờ OT
    public decimal? OT85 { get; set; }
    public decimal? Holiday { get; set; }              // Ngày lễ làm
    public decimal? OTSun { get; set; }                // OT chủ nhật
    
    // Khác
    public bool? NoTax { get; set; }                   // Cam kết không tính thuế
    public decimal? NoTaxIncome { get; set; }          // Thu nhập không tính thuế
    public DateTime? EffectiveDate { get; set; }       // Ngày hiệu lực lương
    public bool? Blocked { get; set; }                 // Locked (không sửa)
    public DateTime? BlockedDate { get; set; }
}
```

#### Payroll (NB - Chi Tiết)
```csharp
public class Payroll : BaseEntity
{
    // Chỉ định
    public string EmployeeCode { get; set; }
    public string MonthYear { get; set; }
    public string BeginPayrollDate { get; set; }
    public string EndingPayrollDate { get; set; }
    
    // Lương tính toán
    public decimal? HourRate { get; set; }             // Lương/giờ
    public decimal? TotalSalary { get; set; }          // Tổng brutto
    public decimal? Net { get; set; }                  // Lương ròng
    
    // Danh mục công
    public decimal? SalaryP { get; set; }              // Lương ngày công
    public decimal? SalaryOT { get; set; }             // Lương OT
    public decimal? SalaryHoliday { get; set; }        // Lương lễ
    public decimal? SalaryOTSun { get; set; }          // Lương OT chủ nhật
    
    // Phụ cấp & thu nhập
    public decimal? SalaryLunch { get; set; }          // Ăn trưa
    public decimal? OtherIncome { get; set; }          // Thu nhập khác
    public decimal? SalaryDiligent { get; set; }       // Phụ cấp siêng năng
    
    // Trừ lương
    public decimal? MinusOther { get; set; }           // Trừ khác
    public decimal? KLLate { get; set; }               // Trừ đi trễ
    
    // Thuế & bảo hiểm
    public decimal? TNCN { get; set; }                 // Thuế (lấy từ SS)
    public decimal? SalaryBHXH { get; set; }           // BHXH
    
    // Chấm công (tham khảo)
    public decimal? P { get; set; }                    // Ngày công
    public decimal? OT { get; set; }                   // Giờ OT
    public decimal? Holiday { get; set; }              // Ngày lễ
    
    public bool? Blocked { get; set; }                 // Locked
}
```

### 2.2 Repository Pattern

```csharp
// IPayrollSSRepository.cs
public interface IPayrollSSRepository : IRepository<PayrollSS, Guid>
{
    Task<PayrollSS> GetByEmployeeCodeAndPeriodAsync(
        string employeeCode, 
        string monthYear,
        bool includeDeleted = false);
    
    Task<List<PayrollSS>> GetByMonthYearAsync(
        string monthYear,
        CancellationToken cancellationToken = default);
    
    Task<List<PayrollSS>> GetByDepartmentAndPeriodAsync(
        string departmentId,
        string monthYear);
}

// Implementation
public class PayrollSSRepository : EfCoreRepository<
    SEVAGOPayrollAPIDbContext,
    PayrollSS,
    Guid>,
    IPayrollSSRepository
{
    public async Task<PayrollSS> GetByEmployeeCodeAndPeriodAsync(
        string employeeCode,
        string monthYear,
        bool includeDeleted = false)
    {
        var query = (await GetQueryableAsync()).AsNoTracking()
            .Where(x => x.EmployeeCode == employeeCode && 
                        x.MonthYear == monthYear);
        
        if (!includeDeleted)
            query = query.Where(x => !x.IsDeleted);
        
        return await AsyncExecuter.FirstOrDefaultAsync(query);
    }
}
```

---

## 3️⃣ Luồng Tính Lương SS (Sổ Sách) - CHI TIẾT

### 3.1 Input Data

```csharp
public class ComputePayrollSSRequest
{
    public string EmployeeCode { get; set; }
    public string MonthYear { get; set; }              // YYYY-MM
    public DateTime BeginPayrollDate { get; set; }
    public DateTime EndingPayrollDate { get; set; }
    public int TotalDaysInPeriod { get; set; }         // Số ngày kỳ
}
```

### 3.2 Step-by-Step Logic

#### **BƯỚC 1: Lấy Lương Cơ Sở Và Xác Định Ngày Hiệu Lực**

```csharp
// Domain Service: IPayrollComputeService
public async Task<PayrollSSDto> ComputeSSAsync(
    ComputePayrollSSRequest request,
    CancellationToken cancellationToken = default)
{
    // Step 1: Lấy lương cơ bản
    var benefit = await _payrollBenefitRepository
        .GetByEmployeeCodeAsync(request.EmployeeCode);
    
    if (benefit == null)
        throw new BusinessException("Employee lương chưa setup");
    
    // Decrypt nếu cần
    var decryptedBenefit = await _encryptionService
        .DecryptPayrollBenefitAsync(benefit);
    
    var result = new PayrollSSDto
    {
        EmployeeCode = request.EmployeeCode,
        MonthYear = request.MonthYear,
        BeginPayrollDate = request.BeginPayrollDate,
        EndingPayrollDate = request.EndingPayrollDate,
    };
    
    // Step 2: Kiểm tra EffectiveDate
    var hasEffectiveDate = decryptedBenefit.EffectiveDate.HasValue &&
                          decryptedBenefit.EffectiveDate >= request.BeginPayrollDate &&
                          decryptedBenefit.EffectiveDate <= request.EndingPayrollDate;
    
    if (hasEffectiveDate)
    {
        // Phân tách 85%
        await HandleSalaryEffectiveDate(
            request,
            decryptedBenefit,
            result);
    }
    else
    {
        // Không có EffectiveDate trong kỳ
        result.TotalSalary = decryptedBenefit.TotalSalary;
        result.BasicSalary = decryptedBenefit.BasicSalary;
        result.TotalSalaryByHour = decryptedBenefit.TotalSalaryByHour;
    }
    
    return result;
}
```

#### **BƯỚC 2: Xử Lý EffectiveDate (Phân Tách 85%)**

```csharp
private async Task HandleSalaryEffectiveDate(
    ComputePayrollSSRequest request,
    PayrollBenefit decryptedBenefit,
    PayrollSSDto result)
{
    var effectiveDate = decryptedBenefit.EffectiveDate.Value;
    
    // Tính số ngày phần 1 (từ BeginDate đến EffectiveDate - 1)
    var daysBeforeEffective = (effectiveDate.Date - request.BeginPayrollDate.Date).Days;
    
    // Tính số ngày phần 2 (từ EffectiveDate đến EndDate)
    var daysAfterEffective = request.TotalDaysInPeriod - daysBeforeEffective;
    
    // Lương cũ (nếu có)
    var oldSalary = decryptedBenefit.TotalSalaryPrevious ?? decryptedBenefit.TotalSalary;
    var newSalary = decryptedBenefit.TotalSalary;
    
    // Công thức: (Lương Cũ / TổngNgày) * SốNgàyCũ + (Lương Mới / TổngNgày) * SốNgàyMới
    var salaryPart1 = (oldSalary / request.TotalDaysInPeriod) * daysBeforeEffective;
    var salaryPart2 = (newSalary / request.TotalDaysInPeriod) * daysAfterEffective;
    
    result.TotalSalary = salaryPart1 + salaryPart2;
    result.TotalSalary85 = salaryPart2;  // Phần 85% (lương mới)
    result.EffectiveDate = effectiveDate;
    
    // Tính lương/giờ theo hai phần (nếu cần)
    result.TotalSalaryByHour = (salaryPart1 / daysBeforeEffective / 8) +
                               (salaryPart2 / daysAfterEffective / 8);
}
```

#### **BƯỚC 3: Tính Thu Nhập Chịu Thuế (TNChiuThue)**

```csharp
// Công thức theo pháp luật Việt Nam
private decimal CalculateTNChiuThue(
    PayrollSSDto payrollSS,
    PayrollBenefit benefit,
    PersonalTaxInfo taxInfo)
{
    var grossIncome = payrollSS.TotalSalary;
    
    // Trừ các khoản không tính thuế (ngoài lương)
    var nonTaxableIncome = payrollSS.OtherIncome ?? 0;
    
    // Trừ giảm trừ gia cảnh cá nhân
    var personalDeduction = 1_600_000m;  // Hoặc từ config
    
    // Trừ giảm trừ gia cảnh con em phụ thuộc
    var dependentDeduction = (taxInfo.NumberOfDependents ?? 0) * 400_000m;
    
    // Trừ các khoản khấu trừ khác (nếu có)
    var otherDeduction = payrollSS.NoTaxIncome ?? 0;
    
    // Tính TNChiuThue
    var tnChiuThue = grossIncome 
                   - nonTaxableIncome 
                   - personalDeduction 
                   - dependentDeduction 
                   - otherDeduction;
    
    return Math.Max(0, tnChiuThue);  // Không âm
}
```

#### **BƯỚC 4: Tính Thuế Thu Nhập Cá Nhân (TNCN)**

```csharp
/// <summary>
/// Tính TNCN theo lũy tiến hoặc quy tắc 10% nếu có cam kết không tính thuế
/// </summary>
private decimal CalculateTNCN(
    decimal tnChiuThue,
    bool? noTax)
{
    // Quy tắc 10% nếu có cam kết không tính thuế
    if (noTax == true)
    {
        return tnChiuThue * 0.10m;
    }
    
    // Bảng lũy tiến
    var taxBrackets = new[]
    {
        (5_000_000m,     0.05m),  // 0 - 5M: 5%
        (10_000_000m,    0.10m),  // 5M - 10M: 10%
        (18_000_000m,    0.15m),  // 10M - 18M: 15%
        (32_000_000m,    0.20m),  // 18M - 32M: 20%
        (52_000_000m,    0.25m),  // 32M - 52M: 25%
        (80_000_000m,    0.30m),  // 52M - 80M: 30%
        (decimal.MaxValue, 0.35m) // 80M+: 35%
    };
    
    decimal tncn = 0;
    decimal previousBracketLimit = 0;
    
    foreach (var (bracketLimit, rate) in taxBrackets)
    {
        if (tnChiuThue <= previousBracketLimit)
            break;
        
        var taxableInThisBracket = Math.Min(tnChiuThue, bracketLimit) - previousBracketLimit;
        tncn += taxableInThisBracket * rate;
        
        previousBracketLimit = bracketLimit;
    }
    
    return Math.Round(tncn, 0);  // Làm tròn
}
```

**Ví Dụ:**
```
TNChiuThue = 8,496,774 VNĐ
Phần 1 (0 - 5M): 5,000,000 × 5% = 250,000
Phần 2 (5M - 8.496M): 3,496,774 × 10% = 349,677
TNCN = 250,000 + 349,677 = 599,677 VNĐ
```

#### **BƯỚC 5: Tính Lương BHXH & Thu Thập BHXH/KPCD**

```csharp
private void CalculateInsurance(
    PayrollSSDto payrollSS,
    PayrollDiligent diligent)
{
    // Lương đóng BHXH (loại bỏ các khoản không tính BHXH)
    var salarybhxh = payrollSS.TotalSalary 
                   - (payrollSS.HealthCareSupport ?? 0); // Nếu có
    
    payrollSS.SalaryBHXH = salarybhxh;
    
    // Lấy từ PayrollDiligent (tính sẵn từ HR hoặc hệ thống bảo hiểm)
    payrollSS.SalaryBHXHCompany = diligent?.BhxhCompany ?? 0;
    payrollSS.SalaryKPCDCompany = diligent?.KpcdCompany ?? 0;
}
```

#### **BƯỚC 6: Lưu Kết Quả**

```csharp
public async Task SavePayrollSSAsync(PayrollSSDto dto)
{
    var entity = new PayrollSS(Guid.NewGuid());
    ObjectMapper.Map(dto, entity);
    
    // Encrypt nếu cần
    if (entity.Type == PayrollBenefitTypeEnum.Confidential)
    {
        await _encryptionService.EncryptPayrollSSAsync(entity);
    }
    
    await _payrollSSRepository.InsertAsync(entity);
    await _unitOfWorkManager.Current.SaveChangesAsync();
}
```

---

## 4️⃣ Luồng Tính Lương NB (Chi Tiết)

### 4.1 Input

```csharp
public class ComputePayrollNBRequest
{
    public string EmployeeCode { get; set; }
    public string MonthYear { get; set; }
    public DateTime BeginPayrollDate { get; set; }
    public DateTime EndingPayrollDate { get; set; }
    public int TotalDaysInPeriod { get; set; }
}
```

### 4.2 Logic Tính Toán

```csharp
public async Task<PayrollDto> ComputeNBAsync(
    ComputePayrollNBRequest request)
{
    // Step 1: Lấy lương cơ bản
    var benefit = await _payrollBenefitRepository
        .GetByEmployeeCodeAsync(request.EmployeeCode);
    
    var decryptedBenefit = await _encryptionService
        .DecryptPayrollBenefitAsync(benefit);
    
    // Step 2: Tính lương/giờ
    var hourRate = CalculateHourRate(
        decryptedBenefit.TotalSalaryNB ?? 0,
        request.TotalDaysInPeriod);
    
    // Step 3: Lấy chấm công
    var attendance = await _attendanceRepository
        .GetByEmployeeCodeAndPeriodAsync(
            request.EmployeeCode,
            request.MonthYear);
    
    // Step 4: Tính lương theo danh mục
    var payroll = new PayrollDto
    {
        EmployeeCode = request.EmployeeCode,
        MonthYear = request.MonthYear,
        HourRate = hourRate,
    };
    
    // Lương cơ bản
    payroll.SalaryP = hourRate * attendance.P * 8;  // P ngày × 8 giờ
    
    // Phụ cấp ăn
    var lunchAllowance = await _formulaService
        .GetAllowanceAsync("LunchAllowance", request.MonthYear);
    payroll.SalaryLunch = attendance.P * lunchAllowance;
    
    // OT
    var otRate = await _formulaService
        .GetAllowanceAsync("OTAllowance", request.MonthYear);
    payroll.SalaryOT = attendance.OT * otRate;
    
    // OT Chủ nhật & Lễ
    payroll.SalaryOTSun = attendance.OTSun * otRate;
    payroll.SalaryHoliday = attendance.Holiday * lunchAllowance;
    
    // Trừ đi trễ
    payroll.KLLate = attendance.Late * hourRate;
    
    // Phụ cấp con (từ config)
    var childAllowance = await _formulaService
        .GetAllowanceAsync("ChildAllowance", request.MonthYear);
    var relativesInfo = await _relativeRepository
        .GetByEmployeeCodeAsync(request.EmployeeCode);
    payroll.ChildAllowance = (relativesInfo?.Count ?? 0) * childAllowance;
    
    // Step 5: Tính Gross
    payroll.TotalSalary = payroll.SalaryP
                        + payroll.SalaryLunch
                        + payroll.SalaryOT
                        + payroll.SalaryOTSun
                        + payroll.SalaryHoliday
                        - payroll.KLLate
                        + payroll.ChildAllowance;
    
    // Step 6: Lấy TNCN từ SS (đã tính)
    var payrollSS = await _payrollSSRepository
        .GetByEmployeeCodeAndPeriodAsync(
            request.EmployeeCode,
            request.MonthYear);
    
    payroll.TNCN = payrollSS?.TNCN ?? 0;
    
    // Step 7: Lấy BHXH
    var diligent = await _payrollDiligentRepository
        .GetByEmployeeCodeAndPeriodAsync(
            request.EmployeeCode,
            request.MonthYear);
    
    payroll.SalaryBHXH = diligent?.BhxhEmployee ?? 0;
    
    // Step 8: Tính Net
    payroll.Net = payroll.TotalSalary 
               - payroll.TNCN 
               - payroll.SalaryBHXH;
    
    return payroll;
}

private decimal CalculateHourRate(
    decimal totalSalaryNB,
    int totalDaysInPeriod)
{
    // Giả sử 8 giờ/ngày
    return totalSalaryNB / totalDaysInPeriod / 8;
}
```

---

## 5️⃣ FormulaConfig & Dynamic Computation

### 5.1 FormulaConfig Entity

```csharp
public class FormulaConfig : BaseEntity
{
    public string FormulaKey { get; set; }        // "LunchAllowance", "OTAllowance"
    public string ViewModelType { get; set; }     // "Allowance", "Deduction", "Tax"
    public string OutputKey { get; set; }         // "salary-lunch", "ot-rate"
    public string Formula { get; set; }           // Công thức (DynamicExpresso)
    public string Description { get; set; }
}
```

### 5.2 Dynamic Formula Evaluation

```csharp
public interface IFormulaEvaluatorService
{
    Task<decimal> EvaluateAsync(
        string formulaKey,
        Dictionary<string, object> context);
}

public class FormulaEvaluatorService : IFormulaEvaluatorService
{
    private readonly IFormulaConfigRepository _formulaRepository;
    private readonly ILogger<FormulaEvaluatorService> _logger;
    
    public async Task<decimal> EvaluateAsync(
        string formulaKey,
        Dictionary<string, object> context)
    {
        var config = await _formulaRepository
            .GetByKeyAsync(formulaKey);
        
        if (config == null)
            throw new BusinessException($"Formula {formulaKey} not found");
        
        try
        {
            // Dùng DynamicExpresso hoặc NLua để evaluate
            var interpreter = new Interpreter();
            
            // Đăng ký context variables
            foreach (var (key, value) in context)
            {
                interpreter.SetVariable(key, value);
            }
            
            var result = interpreter.Eval(config.Formula);
            return Convert.ToDecimal(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Formula evaluation failed: {formulaKey}");
            throw new BusinessException(
                $"Formula evaluation error for {formulaKey}", ex);
        }
    }
}
```

**Ví Dụ Công Thức:**
```
FormulaKey: "LunchAllowance"
Formula: "100000"  // Hằng số

FormulaKey: "OTAllowance"
Formula: "hourRate * 1.5"  // 150% lương giờ

FormulaKey: "ChildAllowance"
Formula: "iif(numberOfChildren > 0, 200000 * numberOfChildren, 0)"
```

---

## 6️⃣ Implementation Pattern (ABP 9)

### 6.1 Application Service Structure

```csharp
public interface IPayrollComputeAppService : IApplicationService
{
    Task ComputeSSAsync(ComputePayrollSSRequest request);
    Task ComputeNBAsync(ComputePayrollNBRequest request);
    Task FinalizeDiligenceAsync(string monthYear);
}

public class PayrollComputeAppService : SEVAGOPayrollAPIAppService, 
    IPayrollComputeAppService
{
    private readonly IPayrollComputeDomainService _domainService;
    private readonly IPayrollSSRepository _ssRepository;
    private readonly IPayrollRepository _nbRepository;
    private readonly IUnitOfWorkManager _uow;
    
    [Authorize]  // ABP Authorization
    public async Task ComputeSSAsync(ComputePayrollSSRequest request)
    {
        using (var uow = _uow.Begin(requiresNew: true))
        {
            try
            {
                // Validate input
                ValidateComputeRequest(request);
                
                // Domain logic
                var payrollSS = await _domainService
                    .ComputeSSAsync(request);
                
                // Save
                var entity = ObjectMapper.Map<PayrollSSDto, PayrollSS>(payrollSS);
                await _ssRepository.InsertAsync(entity);
                
                // Publish domain event (nếu cần)
                await PublishPayrollComputedEventAsync(entity);
                
                await uow.CompleteAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Compute SS failed");
                throw;
            }
        }
    }
    
    private void ValidateComputeRequest(ComputePayrollSSRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EmployeeCode))
            throw new AbpValidationException("EmployeeCode is required");
        
        if (request.BeginPayrollDate >= request.EndingPayrollDate)
            throw new AbpValidationException("Invalid date range");
    }
}
```

### 6.2 Domain Service (Core Logic)

```csharp
public interface IPayrollComputeDomainService : IDomainService
{
    Task<PayrollSSDto> ComputeSSAsync(ComputePayrollSSRequest request);
    Task<PayrollDto> ComputeNBAsync(ComputePayrollNBRequest request);
}

public class PayrollComputeDomainService : DomainService,
    IPayrollComputeDomainService
{
    private readonly IPayrollBenefitRepository _benefitRepository;
    private readonly IFormulaEvaluatorService _formulaEvaluator;
    private readonly IPayrollEncryptionService _encryptionService;
    
    public async Task<PayrollSSDto> ComputeSSAsync(
        ComputePayrollSSRequest request)
    {
        // Step 1: Get & validate benefit
        var benefit = await _benefitRepository
            .GetByEmployeeCodeAsync(request.EmployeeCode);
        
        if (benefit?.IsActive != true)
            throw new BusinessException("Employee benefit not active");
        
        // Step 2: Decrypt if Confidential
        var decrypted = benefit.Type == PayrollBenefitTypeEnum.Confidential
            ? await _encryptionService.DecryptAsync(benefit)
            : benefit;
        
        // Step 3: Initialize result
        var result = new PayrollSSDto();
        
        // Step 4: Handle effective date (85% split)
        if (IsEffectiveDateInPeriod(decrypted.EffectiveDate, request))
        {
            CalculateEffectiveDateSplit(decrypted, request, result);
        }
        else
        {
            result.TotalSalary = decrypted.TotalSalary;
            result.BasicSalary = decrypted.BasicSalary;
        }
        
        // Step 5: Calculate tax details
        result.TNChiuThue = CalculateTaxableIncome(result, decrypted);
        result.TNCN = CalculateTax(result.TNChiuThue, benefit.NoTax);
        
        // Step 6: Get insurance from PayrollDiligent
        var diligent = await GetDiligentAsync(request.EmployeeCode, request.MonthYear);
        result.SalaryBHXH = diligent?.SalaryBHXH;
        
        return result;
    }
    
    private decimal CalculateTaxableIncome(
        PayrollSSDto payroll,
        PayrollBenefit benefit)
    {
        var grossIncome = payroll.TotalSalary;
        var personalDeduction = 1_600_000m;  // From config ideally
        var dependentDeduction = GetDependentDeduction(benefit.EmployeeCode);
        
        return Math.Max(0, grossIncome - personalDeduction - dependentDeduction);
    }
    
    private decimal CalculateTax(decimal taxableIncome, bool? noTax)
    {
        if (noTax == true)
            return taxableIncome * 0.10m;
        
        return CalculateProgressiveTax(taxableIncome);
    }
    
    private decimal CalculateProgressiveTax(decimal income)
    {
        // Implement lũy tiến
        if (income <= 5_000_000) return income * 0.05m;
        if (income <= 10_000_000) 
            return 5_000_000 * 0.05m + (income - 5_000_000) * 0.10m;
        // ... etc
        return 0;
    }
}
```

---

## 7️⃣ Domain Services & Business Logic

### 7.1 PayrollEncryptionService

```csharp
public interface IPayrollEncryptionService
{
    Task<PayrollBenefit> DecryptAsync(PayrollBenefit benefit);
    Task EncryptAsync(PayrollBenefit benefit);
}

public class PayrollEncryptionService : IPayrollEncryptionService
{
    private readonly IEncryptionHelperService _encryptionHelper;
    
    public async Task<PayrollBenefit> DecryptAsync(PayrollBenefit benefit)
    {
        if (benefit.Type != PayrollBenefitTypeEnum.Confidential)
            return benefit;
        
        // Decrypt fields
        benefit.TotalSalary = DecryptDecimal(benefit.TotalSalaryEncrypted);
        benefit.BasicSalary = DecryptDecimal(benefit.BasicSalaryEncrypted);
        // ... decrypt other fields
        
        return benefit;
    }
    
    private decimal DecryptDecimal(string encrypted)
    {
        if (string.IsNullOrEmpty(encrypted))
            return 0;
        
        var decrypted = _encryptionHelper.Decrypt(encrypted);
        return decimal.Parse(decrypted);
    }
}
```

### 7.2 Validation & Error Handling

```csharp
public class PayrollValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    
    public static PayrollValidationResult Success()
        => new() { IsValid = true };
    
    public static PayrollValidationResult Failure(params string[] errors)
        => new() { IsValid = false, Errors = errors.ToList() };
}

public interface IPayrollValidationService
{
    PayrollValidationResult ValidatePayrollComputation(PayrollSSDto payroll);
    PayrollValidationResult ValidateNewBenefit(PayrollBenefitDto benefit);
}

public class PayrollValidationService : IPayrollValidationService
{
    public PayrollValidationResult ValidatePayrollComputation(PayrollSSDto payroll)
    {
        var errors = new List<string>();
        
        // TNCN không âm
        if (payroll.TNCN < 0)
            errors.Add("TNCN không thể âm");
        
        // Lương >= phụ cấp
        if (payroll.TotalSalary < 0)
            errors.Add("Tổng lương không thể âm");
        
        // TNChiuThue hợp lý
        if (payroll.TNChiuThue > payroll.TotalSalary)
            errors.Add("TNChiuThue không thể lớn hơn tổng lương");
        
        return errors.Count > 0
            ? PayrollValidationResult.Failure(errors.ToArray())
            : PayrollValidationResult.Success();
    }
}
```

---

## 8️⃣ Testing Strategy

### 8.1 Unit Tests

```csharp
public class PayrollComputeServiceTests
{
    private readonly IPayrollComputeDomainService _service;
    
    [Fact]
    public async Task CalculateTax_WithNoTaxCommitment_ShouldApply10PercentRule()
    {
        // Arrange
        var taxableIncome = 8_000_000m;
        var noTax = true;
        
        // Act
        var result = _service.CalculateTax(taxableIncome, noTax);
        
        // Assert
        Assert.Equal(800_000, result);  // 8M * 10%
    }
    
    [Fact]
    public async Task CalculateTax_WithProgressiveBrackets_ShouldCalculateCorrectly()
    {
        // Arrange
        var taxableIncome = 8_000_000m;
        var noTax = false;
        
        // Act
        var result = _service.CalculateTax(taxableIncome, noTax);
        
        // Assert (Lũy tiến)
        var expected = 5_000_000 * 0.05m +  // 250K
                      3_000_000 * 0.10m;    // 300K = 550K total
        Assert.Equal(expected, result);
    }
    
    [Fact]
    public async Task ComputeSS_WithEffectiveDate_ShouldSplitSalaryCorrectly()
    {
        // Arrange
        var benefit = new PayrollBenefit
        {
            TotalSalary = 12_000_000,
            TotalSalaryPrevious = 10_000_000,
            EffectiveDate = new DateTime(2025, 1, 15)  // Ngày 15
        };
        
        var request = new ComputePayrollSSRequest
        {
            BeginPayrollDate = new DateTime(2025, 1, 1),
            EndingPayrollDate = new DateTime(2025, 1, 31),
            TotalDaysInPeriod = 31
        };
        
        // Act
        var result = await _service.ComputeSSAsync(request);
        
        // Assert
        // Phần 1 (14 ngày): 10M/31 * 14 = 4.516M
        // Phần 2 (17 ngày): 12M/31 * 17 = 6.581M
        // Total: 11.097M
        Assert.Equal(11_096_774m, Math.Round(result.TotalSalary, 0));
    }
}
```

### 8.2 Integration Tests

```csharp
[Collection("PayrollIntegration")]
public class PayrollComputeAppServiceIntegrationTests : 
    SEVAGOPayrollAPIApplicationTestBase
{
    private readonly IPayrollComputeAppService _appService;
    private readonly IPayrollSSRepository _ssRepository;
    
    [Fact]
    public async Task ComputeSS_ShouldSaveToDatabase()
    {
        // Arrange
        await CreateTestEmployeeAsync("EMP001");
        await CreateTestBenefitAsync("EMP001", 10_000_000);
        
        var request = new ComputePayrollSSRequest
        {
            EmployeeCode = "EMP001",
            MonthYear = "2025-01",
            BeginPayrollDate = new DateTime(2025, 1, 1),
            EndingPayrollDate = new DateTime(2025, 1, 31),
            TotalDaysInPeriod = 31
        };
        
        // Act
        await _appService.ComputeSSAsync(request);
        
        // Assert
        var saved = await _ssRepository
            .GetByEmployeeCodeAndPeriodAsync("EMP001", "2025-01");
        
        Assert.NotNull(saved);
        Assert.Equal(10_000_000m, saved.TotalSalary);
    }
}
```

---

## 9️⃣ Common Mistakes & Pitfalls

### ❌ Lỗi 1: Quên xử lý EffectiveDate

```csharp
// SAI ❌
var totalSalary = benefit.TotalSalary;  // Không kiểm tra EffectiveDate

// ĐÚNG ✅
var totalSalary = IsEffectiveDateInPeriod(benefit.EffectiveDate, period)
    ? CalculateSalaryWithSplit(benefit, period)
    : benefit.TotalSalary;
```

### ❌ Lỗi 2: Không Decrypt mã hóa trước khi sử dụng

```csharp
// SAI ❌
var salary = benefit.TotalSalaryEncrypted;  // Vẫn là chuỗi mã hóa

// ĐÚNG ✅
var decrypted = await _encryptionService.DecryptAsync(benefit);
var salary = decrypted.TotalSalary;
```

### ❌ Lỗi 3: Tính thuế sai logic lũy tiến

```csharp
// SAI ❌
var tncn = taxableIncome * 0.15m;  // Toàn bộ × 15%

// ĐÚNG ✅
var tncn = (5_000_000 * 0.05m) +         // Phần 5M đầu
          ((taxableIncome - 5_000_000) * 0.10m);  // Phần còn lại
```

### ❌ Lỗi 4: Không kiểm tra duplicate payroll

```csharp
// SAI ❌
await _repository.InsertAsync(payroll);  // Có thể duplicate

// ĐÚNG ✅
var existing = await _repository
    .GetByEmployeeCodeAndPeriodAsync(employeeCode, monthYear);

if (existing != null)
    throw new BusinessException("Payroll already exists");

await _repository.InsertAsync(payroll);
```

### ❌ Lỗi 5: Không handle transaction

```csharp
// SAI ❌
await _repo1.InsertAsync(payrollSS);
await _repo2.InsertAsync(payroll);
// Nếu repo2 fail, repo1 vẫn lưu → dữ liệu không consistent

// ĐÚNG ✅
using (var uow = _uow.Begin(requiresNew: true))
{
    try
    {
        await _repo1.InsertAsync(payrollSS);
        await _repo2.InsertAsync(payroll);
        await uow.CompleteAsync();
    }
    catch
    {
        await uow.RollbackAsync();
        throw;
    }
}
```

### ❌ Lỗi 6: Lấy dữ liệu cũ từ cache

```csharp
// SAI ❌
var benefit = _benefitCache.Get(employeeCode);  // Có thể cũ

// ĐÚNG ✅
var benefit = await _repository.GetByEmployeeCodeAsync(employeeCode);
// Hoặc invalidate cache sau khi update
await _benefitCache.RemoveAsync(employeeCode);
```

---

## 🎯 Summary & Checklist

### Trước Khi Deploy

- [ ] Unit test coverage ≥ 80%
- [ ] Integration test cho core workflows (SS, NB, Finalize)
- [ ] Validate với 100+ samples từ legacy system
- [ ] Performance test (compute 1000 employees < 5s)
- [ ] Security review (encryption at rest, RBAC)
- [ ] Data migration script tested
- [ ] Rollback plan ready
- [ ] Monitoring & alerting setup

### Khi Coding

- [ ] Luôn decrypt trước khi sử dụng encrypted fields
- [ ] Kiểm tra EffectiveDate trong mọi trường hợp
- [ ] Dùng Transaction cho multi-table operations
- [ ] Validate input từ request
- [ ] Publish domain events cho audit/notification
- [ ] Log chi tiết compute steps
- [ ] Handle edge cases (new joiners, leavers, retroactive)

---

## 📚 References

- ABP Framework 9.0 Docs: https://docs.abp.io/
- Vietnam Tax Rules: Luật Thuế Thu Nhập Cá Nhân (cập nhật 2024)
- Encryption: AES-256 asymmetric recommended
- Database: SQL Server 2019+ recommended for Always Encrypted support

---

**Document Version**: 1.0  
**Last Updated**: 10/12/2025  
**Author**: BA + TechLead Team
Copilot said: ````markdown name=docs/Developer_Guide_Payroll_ABP9.md # 📘 Hướng Dẫn Tính Lương
docs/Developer_Guide_Payroll_ABP9.md
# 📘 Hướng Dẫn Tính Lương Chi Tiết Cho Developer
## ABP 9 + Công Thức Tính Lương + Best Practices

Dành cho: Developer  
Scope: Tính lương SS, NB, công thức, implementation, testing  
Ngôn ngữ: Tiếng Việt + Code (C#)  
Bạn có muốn mình tạo pull request thêm file này vào thư mục docs của repo hiện tại không? Nếu có, bạn cho mình biết repo owner/repo và nhánh base để mình mở PR.

You said: # 🚀 QUICK REFERENCE - TÍNH LƯƠNG
# 🚀 QUICK REFERENCE - TÍNH LƯƠNG PAYROLL
## Cheat Sheet Cho Dev

**Mục đích**: Nhanh chóng tra cứu công thức, code pattern, lỗi thường gặp  
**Cập nhật**: 10/12/2025

---

## 📊 Công Thức Tính Nhanh

### SS (Sổ Sách)

| Bước | Công Thức | Code C# |
|------|-----------|---------|
| 1 | Lương tổng = TotalSalary từ PayrollBenefit | `benefit.TotalSalary` |
| 2 | Nếu có EffectiveDate | `HandleSalaryEffectiveDate(...)` |
| 3 | TNChiuThue = Tổng - Giảm trừ | `tnChiuThue = gross - 1.6M - (children * 400K)` |
| 4 | TNCN (10% rule) | `tncn = tnChiuThue * 0.10m` |
| 5 | TNCN (lũy tiến) | `CalculateProgressiveTax(tnChiuThue)` |
| 6 | BHXH | `diligent.BhxhCompany` |

**Ví dụ Nhanh:**
```
Lương: 10M
TNChiuThue: 10M - 1.6M - 0.8M = 7.6M
TNCN (10%): 7.6M × 10% = 760K
TNCN (lũy tiến): 5M × 5% + 2.6M × 10% = 760K
```

### NB (Chi Tiết)

| Danh Mục | Công Thức | C# |
|----------|-----------|-----|
| Lương cơ bản | HourRate × P × 8 | `hourRate * attendance.P * 8` |
| Ăn trưa | P × 100K | `attendance.P * 100_000` |
| OT | OT giờ × 50K | `attendance.OT * 50_000` |
| Chủ nhật | OTSun × 50K | `attendance.OTSun * 50_000` |
| Lễ | Holiday × 100K | `attendance.Holiday * 100_000` |
| Trừ đi trễ | Late × HourRate | `attendance.Late * hourRate` |
| **TỔNG** | **= Tất cả trên** | `Sum(...)` |
| **Net** | **Tổng - TNCN - BHXH** | `total - tncn - bhxh` |

---

## 🔑 Key Classes & Methods

### Domain Service

```csharp
// Compute SS
Task<PayrollSSDto> ComputeSSAsync(ComputePayrollSSRequest request)

// Compute NB
Task<PayrollDto> ComputeNBAsync(ComputePayrollNBRequest request)

// Calculate Tax
decimal CalculateTax(decimal tnChiuThue, bool? noTax)

// Handle EffectiveDate
void HandleSalaryEffectiveDate(...)
```

### Application Service

```csharp
// Main compute
Task ComputeSSAsync(ComputePayrollSSRequest request)
Task ComputeNBAsync(ComputePayrollNBRequest request)

// Admin operations
Task FinalizeDiligenceAsync(string monthYear)
Task DeletePayrollAsync(Guid id)
Task LockPayrollPeriodAsync(string monthYear)
```

### Repository

```csharp
// Common queries
Task<PayrollBenefit> GetByEmployeeCodeAsync(string code)
Task<PayrollSS> GetByEmployeeCodeAndPeriodAsync(code, monthYear)
Task<List<PayrollSS>> GetByMonthYearAsync(monthYear)
Task<PayrollDiligent> GetByEmployeeCodeAndPeriodAsync(code, monthYear)
```

---

## 💰 Bảng Lũy Tiến Thuế (2024)

```
Từ 0        - 5.000.000    : 5%
Từ 5.000.001 - 10.000.000  : 10%
Từ 10.000.001 - 18.000.000 : 15%
Từ 18.000.001 - 32.000.000 : 20%
Từ 32.000.001 - 52.000.000 : 25%
Từ 52.000.001 - 80.000.000 : 30%
Từ 80.000.001+             : 35%
```

**Code:**
```csharp
decimal CalculateProgressiveTax(decimal income)
{
    if (income <= 5M) return income * 0.05m;
    if (income <= 10M) return 5M * 0.05m + (income - 5M) * 0.10m;
    if (income <= 18M) return 5M * 0.05m + 5M * 0.10m + (income - 10M) * 0.15m;
    // ... continue
}
```

---

## 📝 EffectiveDate (85% Rule)

**Khi EffectiveDate nằm trong kỳ:**

```
Kỳ: 01/01 - 31/01 (31 ngày)
EffectiveDate: 15/01
OldSalary: 10M, NewSalary: 12M

Phần 1 (01/01 - 14/01, 14 ngày):
  = (10M / 31) × 14 = 4,516,129

Phần 2 (15/01 - 31/01, 17 ngày):
  = (12M / 31) × 17 = 6,580,645

Tổng = 11,096,774

TotalSalary85 = Phần 2 = 6,580,645  // Dùng cho tính Bonus 85%
```

---

## 🔐 Encryption Workflow

### Decrypt (Đọc lương mã hóa)

```csharp
// 1. Lấy benefit
var benefit = await _repo.GetByEmployeeCodeAsync(code);

// 2. Kiểm tra type
if (benefit.Type == PayrollBenefitTypeEnum.Confidential)
{
    // 3. Decrypt
    benefit = await _encryptionService.DecryptAsync(benefit);
}

// 4. Sử dụng
var salary = benefit.TotalSalary;  // Đã decrypt
```

### Encrypt (Lưu lương mã hóa)

```csharp
var benefit = new PayrollBenefit { ... };

if (benefit.Type == PayrollBenefitTypeEnum.Confidential)
{
    await _encryptionService.EncryptAsync(benefit);
}

await _repo.InsertAsync(benefit);
```

---

## ✅ Validation Checklist

```csharp
// Trước tính lương
□ EmployeeCode exist & active?
□ Benefit data complete?
□ Period valid (Begin < End)?
□ Duplicate check?

// Trong tính lương
□ EffectiveDate handled?
□ Decryption ok?
□ Attendance data available?
□ Tax config loaded?

// Sau tính lương
□ TNCN >= 0?
□ TotalSalary >= 0?
□ TNChiuThue <= TotalSalary?
□ Net >= 0?
□ Transaction completed?
```

---

## ⚠️ Lỗi Thường Gặp

| Lỗi | Nguyên Nhân | Fix |
|-----|-----------|-----|
| Lương = 0 | EffectiveDate error | Check date logic |
| Encrypt fail | Missing password | Setup encryption service |
| Duplicate | Không check exist | Add `GetByEmployeeAndPeriod` |
| Tax sai | Quên 10% rule | Check `NoTax` flag |
| BHXH miss | Không lấy Diligent | Join với PayrollDiligent |
| Transaction fail | No UoW | Wrap trong `using (var uow = ...)` |

---

## 🎯 Step-by-Step: Tính SS Đơn Giản

```csharp
// 1. Request
var req = new ComputePayrollSSRequest 
{ 
    EmployeeCode = "E001",
    MonthYear = "2025-01"
};

// 2. Get benefit
var benefit = await _benefitRepo.GetByEmployeeCodeAsync("E001");
var decrypted = benefit.Type == Confidential 
    ? await _encryption.DecryptAsync(benefit)
    : benefit;

// 3. Check effective date
var inPeriod = decrypted.EffectiveDate >= req.BeginDate 
            && decrypted.EffectiveDate <= req.EndDate;

// 4. Calc salary
var salary = inPeriod 
    ? CalculateSplit(decrypted, req)
    : decrypted.TotalSalary;

// 5. Calc tax
var tnChiuThue = salary - 1_600_000 - (children * 400_000);
var tncn = decrypted.NoTax == true 
    ? tnChiuThue * 0.10m
    : CalculateProgressiveTax(tnChiuThue);

// 6. Create entity
var payrollSS = new PayrollSS
{
    EmployeeCode = "E001",
    TotalSalary = salary,
    TNChiuThue = tnChiuThue,
    TNCN = tncn
};

// 7. Save
await _repo.InsertAsync(payrollSS);
```

---

## 📊 Data Flow Diagram

```
┌─────────────────────┐
│ Benefit (Lương CB)  │
│  • TotalSalary      │
│  • EffectiveDate    │
│  • NoTax            │
└──────────┬──────────┘
           │
           ▼
  ┌────────────────────┐
  │ Check EffectiveDate│
  │  (85% Rule)        │
  └────────┬───────────┘
           │
           ▼
  ┌─────────────────────┐
  │ Calc TNChiuThue     │
  │ (Gross - Deduction) │
  └────────┬────────────┘
           │
           ▼
  ┌─────────────────────┐
  │ Calc TNCN           │
  │ (10% or Progressive)│
  └────────┬────────────┘
           │
           ▼
  ┌─────────────────────┐
  │ Calc BHXH/KPCD      │
  │ (From Diligent)     │
  └────────┬────────────┘
           │
           ▼
  ┌─────────────────────┐
  │ PayrollSS (Saved)   │
  │ ✓ TotalSalary       │
  │ ✓ TNChiuThue        │
  │ ✓ TNCN              │
  │ ✓ BHXH              │
  └─────────────────────┘
```

---

## 📱 DTOs Cấu Trúc

```csharp
// Request
public class ComputePayrollSSRequest
{
    public string EmployeeCode { get; set; }
    public string MonthYear { get; set; }
    public DateTime BeginPayrollDate { get; set; }
    public DateTime EndingPayrollDate { get; set; }
    public int TotalDaysInPeriod { get; set; }
}

// Response
public class PayrollSSDto
{
    public string EmployeeCode { get; set; }
    public decimal? TotalSalary { get; set; }
    public decimal? TNChiuThue { get; set; }
    public decimal? TNCN { get; set; }
    public decimal? SalaryBHXH { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public bool? NoTax { get; set; }
}
```

---

## 🧪 Test Template

```csharp
[Fact]
public async Task ComputeSS_ValidInput_ReturnsCorrectResult()
{
    // Arrange
    var benefit = new PayrollBenefit 
    { 
        TotalSalary = 10_000_000,
        EffectiveDate = null
    };
    
    var request = new ComputePayrollSSRequest
    {
        EmployeeCode = "E001",
        MonthYear = "2025-01",
        TotalDaysInPeriod = 31
    };
    
    // Act
    var result = await _service.ComputeSSAsync(request);
    
    // Assert
    Assert.Equal(10_000_000, result.TotalSalary);
    Assert.NotNull(result.TNCN);
    Assert.True(result.TNCN > 0);
}
```

---

## 🔗 Useful Links

| Topic | Link |
|-------|------|
| ABP Repo Pattern | https://docs.abp.io/en/abp/latest/Repositories |
| ABP DomainService | https://docs.abp.io/en/abp/latest/Domain-Services |
| DynamicExpresso | https://dynamicexpresso.net/ |
| Encryption (ABP) | ABP docs → Data Protection |

---

## 💡 Pro Tips

1. **Always decrypt first** - Mã hóa = string, cần decrypt trước dùng
2. **Check EffectiveDate** - 85% rule không phải optional
3. **Use UoW for transactions** - Multi-table operations cần rollback
4. **Test with edge cases** - Join date, resign date, mid-month changes
5. **Publish events** - Mỗi compute thành công publish event để audit
6. **Log details** - Compute steps, parameters, results
7. **Cache invalidation** - Sau update benefit, invalidate cache
8. **Validate early** - Request validation trước domain logic

---

**Version**: 1.0 | **Updated**: 10/12/2025