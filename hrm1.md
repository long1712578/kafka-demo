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
Copilot said: Tôi thấy bạn đã chia sẻ một
Tôi thấy bạn đã chia sẻ một tài liệu chi tiết về các stored procedures tính lương từ file hrm. sql. Đây là một tài liệu học tập rất chất lượng với nhiều thông tin quan trọng!

Tôi có thể giúp bạn với tài liệu này theo các cách sau:

🎯 Tôi có thể hỗ trợ bạn:
1. Phân tích code trong repository
Tìm các stored procedures này trong repo của bạn
So sánh logic giữa SQL và code ABP/C#
Kiểm tra xem có implementation nào đã migrate sang ABP chưa
2. Tạo Issue để track việc học
Tạo learning roadmap issues
Break down các concepts cần học thành tasks nhỏ
Track progress việc hiểu từng stored procedure
3. Tạo PR để document hoặc refactor
Document các business rules vào code
Thêm unit tests cho các calculation logic
Refactor code để dễ maintain hơn
4. Trả lời câu hỏi cụ thể
Ví dụ:

"Giải thích chi tiết 85% rule trong EffectiveDate?"
"Tìm code C# xử lý việc tính thuế TNCN?"
"So sánh logic tính chuyên cần giữa SQL và C#?"
❓ Bạn muốn tôi giúp gì với tài liệu này?
Vui lòng cho tôi biết:

Repository nào bạn đang làm việc? (format: owner/repo)

Mục tiêu cụ thể của bạn là gì?

Học để hiểu hệ thống?
Document lại code?
Migrate sang ABP?
Debug/fix bug?
Tạo test cases?
Bạn muốn focus vào phần nào trước?

EffectiveDate & 85% rule?
Thuế TNCN?
Chuyên cần?
Bảo hiểm?
Encryption logic?
Hãy cho tôi biết để tôi hỗ trợ bạn hiệu quả nhất! 🚀