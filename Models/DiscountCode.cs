public class DiscountCode
{
    public int Id { get; set; }

    // รหัสคูปอง เช่น SAVE10
    public string Code { get; set; } = default!;  // UNIQUE, UPPERCASE แนะนำบังคับเป็นตัวพิมพ์ใหญ่

    // ส่วนลด
    public decimal Amount { get; set; }          // มูลค่าส่วนลด (เช่น 50 บาท หรือ 10%)
    public bool IsPercent { get; set; }          // true = คิดเป็น %, false = เป็นจำนวนเงิน

    // เงื่อนไข/ลิมิต
    public int? MaxUses { get; set; }            // ใช้ได้มากสุดทั้งหมด (null = ไม่จำกัด)
    public int UsedCount { get; set; }           // ถูกใช้ไปแล้วกี่ครั้ง
    public int? PerUserLimit { get; set; }       // จำกัดต่อผู้ใช้แต่ละคน (null = ไม่จำกัด)

    public decimal? MinOrderAmount { get; set; } // ยอดขั้นต่ำถึงจะใช้ได้ (เช่น 300 บาท)

    public DateTime? StartAt { get; set; }       // วันเริ่ม
    public DateTime? EndAt { get; set; }         // วันสิ้นสุด
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<DiscountRedemption> Redemptions { get; set; } = new List<DiscountRedemption>();
}

public class DiscountRedemption
{
    public int Id { get; set; }
    public int DiscountCodeId { get; set; }
    public int UserId { get; set; }

    public decimal OrderAmount { get; set; }     // ยอดก่อนหัก
    public decimal DiscountApplied { get; set; } // มูลค่าส่วนลดที่ใช้จริง
    public decimal FinalAmount { get; set; }     // ยอดสุทธิหลังหัก

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DiscountCode DiscountCode { get; set; } = default!;
}

public class CreateDiscountDto
{
    public string Code { get; set; } = default!; // เช่น "SAVE10"
    public decimal Amount { get; set; }
    public bool IsPercent { get; set; }
    public int? MaxUses { get; set; }
    public int? PerUserLimit { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PreviewDiscountDto
{
    public string Code { get; set; } = default!;
    public int UserId { get; set; }
    public decimal OrderAmount { get; set; }
}

public class RedeemDiscountDto
{
    public string Code { get; set; } = default!;
    public int UserId { get; set; }
    public decimal OrderAmount { get; set; }
}
