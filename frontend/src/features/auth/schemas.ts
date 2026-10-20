import { z } from "zod";

// Single source of truth for the ASP.NET Identity password policy
// (integration doc section 8). Used by both the Zod schema and the live
// checklist in the register form.
const MIN_LENGTH = 8;
const UPPER_RE = /[A-Z]/;
const LOWER_RE = /[a-z]/;
const DIGIT_RE = /\d/;
const NON_ALNUM_RE = /[^A-Za-z0-9]/;

export type PasswordRule = {
  key: string;
  label: string;
  test: (value: string) => boolean;
};

export const passwordRules: PasswordRule[] = [
  { key: "length", label: "At least 8 characters", test: (v) => v.length >= MIN_LENGTH },
  { key: "uppercase", label: "One uppercase letter", test: (v) => UPPER_RE.test(v) },
  { key: "lowercase", label: "One lowercase letter", test: (v) => LOWER_RE.test(v) },
  { key: "digit", label: "One digit", test: (v) => DIGIT_RE.test(v) },
  { key: "symbol", label: "One non-alphanumeric character", test: (v) => NON_ALNUM_RE.test(v) },
];

export const passwordSchema = z
  .string()
  .min(MIN_LENGTH, "Password must be at least 8 characters.")
  .regex(UPPER_RE, "Password must contain an uppercase letter.")
  .regex(LOWER_RE, "Password must contain a lowercase letter.")
  .regex(DIGIT_RE, "Password must contain a digit.")
  .regex(NON_ALNUM_RE, "Password must contain a non-alphanumeric character.");

export const loginSchema = z.object({
  email: z.email("Enter a valid email address."),
  password: z.string().min(1, "Password is required."),
});
export type LoginInput = z.infer<typeof loginSchema>;

export const registerSchema = z
  .object({
    fullName: z
      .string()
      .min(2, "Full name must be at least 2 characters.")
      .max(100, "Full name must be 100 characters or fewer."),
    email: z.email("Enter a valid email address."),
    password: passwordSchema,
    applyAsAgent: z.boolean(),
    categoryIds: z.array(z.number().int()),
    agentMessage: z
      .string()
      .max(1000, "Message must be 1000 characters or fewer.")
      .optional()
      .or(z.literal("")),
  })
  .superRefine((data, ctx) => {
    if (data.applyAsAgent && data.categoryIds.length === 0) {
      ctx.addIssue({
        code: "custom",
        path: ["categoryIds"],
        message: "Select at least one category.",
      });
    }
  });
export type RegisterInput = z.infer<typeof registerSchema>;
