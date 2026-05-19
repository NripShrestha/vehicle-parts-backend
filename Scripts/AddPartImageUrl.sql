-- Run this in pgAdmin / psql if "dotnet ef database update" cannot connect.
-- Adds the image URL column to Parts (safe to run more than once).

ALTER TABLE "Parts"
ADD COLUMN IF NOT EXISTS "ImageUrl" character varying(500) NULL;
