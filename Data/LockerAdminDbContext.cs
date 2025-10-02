using System;
using System.Collections.Generic;
using ICT371525Y_School_Locker_App.Models;
using Microsoft.EntityFrameworkCore;

namespace ICT371525Y_School_Locker_App.Data;

public partial class LockerAdminDbContext : DbContext
{
    public LockerAdminDbContext()
    {
    }

    public LockerAdminDbContext(DbContextOptions<LockerAdminDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<Grade> Grades { get; set; }

    public virtual DbSet<Locker> Lockers { get; set; }

    public virtual DbSet<LockerWaitingList> LockerWaitingLists { get; set; }

    public virtual DbSet<Parent> Parents { get; set; }

    public virtual DbSet<ParentStudentStaging> ParentStudentStagings { get; set; }

    public virtual DbSet<School> Schools { get; set; }

    public virtual DbSet<SchoolGrade> SchoolGrades { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=127.0.0.1,1433;Database=LockerAdminDB;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.AdminId).HasName("PK__Admins__719FE4E8761BEA56");

            entity.HasIndex(e => e.SchoolId, "IX_Admins_SchoolID");

            entity.Property(e => e.AdminId).HasColumnName("AdminID");
            entity.Property(e => e.AdminEmail)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.AdminName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.SchoolId).HasColumnName("SchoolID");

            entity.HasOne(d => d.School).WithMany(p => p.Admins)
                .HasForeignKey(d => d.SchoolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Admins__SchoolID__05D8E0BE");
        });

        modelBuilder.Entity<Grade>(entity =>
        {
            entity.HasKey(e => e.GradesId).HasName("PK__Grades__931A40BF9201EDBC");

            entity.Property(e => e.GradesId).HasColumnName("GradesID");
            entity.Property(e => e.GradeName)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Locker>(entity =>
        {
            entity.HasKey(e => e.LockerId).HasName("PK__Lockers__50B47B39EFF0DAC5");

            entity.HasIndex(e => e.GradeId, "IX_Lockers_GradeID");

            entity.HasIndex(e => e.LockerNumber, "IX_Lockers_LockerNumber");

            entity.HasIndex(e => e.SchoolId, "IX_Lockers_SchoolID");

            entity.HasIndex(e => e.StudentIdCurrentBookingYear, "IX_Lockers_StudentID");

            entity.Property(e => e.LockerId).HasColumnName("LockerID");
            entity.Property(e => e.AssignedDate).HasColumnType("datetime");
            entity.Property(e => e.CurrentBookingYear).HasDefaultValue(false);
            entity.Property(e => e.FollowingBookingYear).HasDefaultValue(false);
            entity.Property(e => e.GradeId).HasColumnName("GradeID");
            entity.Property(e => e.LockerNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SchoolId).HasColumnName("SchoolID");

            entity.HasOne(d => d.Grade).WithMany(p => p.Lockers)
                .HasForeignKey(d => d.GradeId)
                .HasConstraintName("FK__Lockers__GradeID__3F466844");

            entity.HasOne(d => d.School).WithMany(p => p.Lockers)
                .HasForeignKey(d => d.SchoolId)
                .HasConstraintName("FK__Lockers__SchoolI__3E52440B");

            entity.HasOne(d => d.StudentIdCurrentBookingYearNavigation).WithMany(p => p.Lockers)
                .HasForeignKey(d => d.StudentIdCurrentBookingYear)
                .HasConstraintName("FK__Lockers__Student__403A8C7D");
        });

        modelBuilder.Entity<LockerWaitingList>(entity =>
        {
            entity.HasKey(e => e.WaitingListId).HasName("PK__LockerWa__3003F4B7C240BE5A");

            entity.ToTable("LockerWaitingList");

            entity.HasIndex(e => e.GradeId, "IX_LockerWaitingList_GradeID");

            entity.HasIndex(e => e.SchoolId, "IX_LockerWaitingList_SchoolID");

            entity.HasIndex(e => e.StudentId, "IX_LockerWaitingList_StudentID");

            entity.Property(e => e.WaitingListId).HasColumnName("WaitingListID");
            entity.Property(e => e.AppliedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.GradeId).HasColumnName("GradeID");
            entity.Property(e => e.SchoolId).HasColumnName("SchoolID");
            entity.Property(e => e.StudentId).HasColumnName("StudentID");

            entity.HasOne(d => d.Grade).WithMany(p => p.LockerWaitingLists)
                .HasForeignKey(d => d.GradeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__LockerWai__Grade__7C4F7684");

            entity.HasOne(d => d.School).WithMany(p => p.LockerWaitingLists)
                .HasForeignKey(d => d.SchoolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__LockerWai__Schoo__7B5B524B");

            entity.HasOne(d => d.Student).WithMany(p => p.LockerWaitingLists)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__LockerWai__Stude__7D439ABD");
        });

        modelBuilder.Entity<Parent>(entity =>
        {
            entity.HasKey(e => e.ParentId).HasName("PK__Parents__D339510FE54A5A73");

            entity.HasIndex(e => e.ParentIdnumber, "IX_Parents_ParentIDNumber");

            entity.Property(e => e.ParentId).HasColumnName("ParentID");
            entity.Property(e => e.ParentEmail)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ParentHomeAddress)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.ParentIdnumber).HasColumnName("ParentIDNumber");
            entity.Property(e => e.ParentName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ParentSurname)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ParentTitle)
                .HasMaxLength(10)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ParentStudentStaging>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("ParentStudentStaging");

            entity.Property(e => e.ParentEmailAddress).HasMaxLength(255);
            entity.Property(e => e.ParentHomeAddress).HasMaxLength(500);
            entity.Property(e => e.ParentIdnumber)
                .HasMaxLength(100)
                .HasColumnName("ParentIDNumber");
            entity.Property(e => e.ParentName).HasMaxLength(100);
            entity.Property(e => e.ParentPhoneNumber).HasMaxLength(100);
            entity.Property(e => e.ParentSurname).HasMaxLength(100);
            entity.Property(e => e.ParentTitle)
                .HasMaxLength(100)
                .HasColumnName("Parent_Title");
            entity.Property(e => e.StudentGrade).HasMaxLength(100);
            entity.Property(e => e.StudentName).HasMaxLength(100);
            entity.Property(e => e.StudentSchoolNumber).HasMaxLength(100);
            entity.Property(e => e.StudentSurname).HasMaxLength(100);
        });

        modelBuilder.Entity<School>(entity =>
        {
            entity.HasKey(e => e.SchoolId).HasName("PK__Schools__3DA4677B8917E88E");

            entity.Property(e => e.SchoolId).HasColumnName("SchoolID");
            entity.Property(e => e.SchoolName)
                .HasMaxLength(255)
                .IsUnicode(false);
        });

        modelBuilder.Entity<SchoolGrade>(entity =>
        {
            entity.HasKey(e => e.SchoolGrade1).HasName("PK__SchoolGr__3A354398DBF4D469");

            entity.HasIndex(e => e.GradeId, "IX_SchoolGrades_GradeID");

            entity.HasIndex(e => e.SchoolId, "IX_SchoolGrades_SchoolID");

            entity.Property(e => e.SchoolGrade1).HasColumnName("SchoolGrade");
            entity.Property(e => e.GradeId).HasColumnName("GradeID");
            entity.Property(e => e.SchoolId).HasColumnName("SchoolID");

            entity.HasOne(d => d.Grade).WithMany(p => p.SchoolGrades)
                .HasForeignKey(d => d.GradeId)
                .HasConstraintName("FK__SchoolGra__Grade__440B1D61");

            entity.HasOne(d => d.School).WithMany(p => p.SchoolGrades)
                .HasForeignKey(d => d.SchoolId)
                .HasConstraintName("FK__SchoolGra__Schoo__4316F928");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__Students__32C52A792B5A0B8C");

            entity.HasIndex(e => e.ParentId, "IX_Students_ParentID");

            entity.HasIndex(e => e.SchoolId, "IX_Students_SchoolID");

            entity.HasIndex(e => e.StudentSchoolNumber, "IX_Students_StudentSchoolNumber");

            entity.Property(e => e.StudentId).HasColumnName("StudentID");
            entity.Property(e => e.GradesId).HasColumnName("GradesID");
            entity.Property(e => e.ParentId).HasColumnName("ParentID");
            entity.Property(e => e.SchoolId).HasColumnName("SchoolID");
            entity.Property(e => e.StudentName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.StudentSchoolNumber)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.Grades).WithMany(p => p.Students)
                .HasForeignKey(d => d.GradesId)
                .HasConstraintName("FK_Students_Grades");

            entity.HasOne(d => d.Parent).WithMany(p => p.Students)
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("FK__Students__Parent__02FC7413");

            entity.HasOne(d => d.School).WithMany(p => p.Students)
                .HasForeignKey(d => d.SchoolId)
                .HasConstraintName("FK__Students__School__3B75D760");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
