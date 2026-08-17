import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AiAnalysisCard } from "./AiAnalysisCard";
const analyze = vi.fn();
vi.mock("../api/ai", () => ({
  analyzeTicketAsync: (...args: unknown[]) => analyze(...args),
}));
const result = {
  summary: "Restarting the print spooler may restore service.",
  recommendedCategoryId: 1,
  recommendedCategoryName: "Hardware",
  recommendedPriorityId: 2,
  recommendedPriorityName: "High",
  troubleshootingSuggestions: ["Check power", "Restart spooler"],
  disclaimer:
    "AI-generated suggestions may be inaccurate. Review before applying.",
};
describe("AiAnalysisCard", () => {
  beforeEach(() => analyze.mockReset());
  it("does not call automatically and renders an accessible action", () => {
    render(<AiAnalysisCard ticketId="abc" />);
    expect(
      screen.getByRole("button", { name: "Analyze Ticket" }),
    ).toBeInTheDocument();
    expect(analyze).not.toHaveBeenCalled();
    expect(
      screen.queryByRole("button", { name: /apply/i }),
    ).not.toBeInTheDocument();
  });
  it("prevents duplicates and renders advisory output as text", async () => {
    analyze.mockResolvedValue(result);
    render(<AiAnalysisCard ticketId="abc" />);
    await userEvent.click(
      screen.getByRole("button", { name: "Analyze Ticket" }),
    );
    expect(analyze).toHaveBeenCalledOnce();
    expect(analyze.mock.calls[0][0]).toBe("abc");
    expect(analyze.mock.calls[0][1]).toBeInstanceOf(AbortSignal);
    expect(await screen.findByText(result.summary)).toBeInTheDocument();
    expect(screen.getByText("Hardware")).toBeInTheDocument();
    expect(screen.getByText("High")).toBeInTheDocument();
    expect(screen.getByText("Check power")).toBeInTheDocument();
    expect(screen.getAllByText(/Review before applying/).length).toBeGreaterThan(0);
  });
  it("renders unknown recommendation fallbacks", async () => {
    analyze.mockResolvedValueOnce({
      ...result,
      recommendedCategoryName: null,
      recommendedPriorityName: null,
    });
    const view = render(<AiAnalysisCard ticketId="abc" />);
    await userEvent.click(
      screen.getByRole("button", { name: "Analyze Ticket" }),
    );
    expect(
      await screen.findByText("No category suggestion"),
    ).toBeInTheDocument();
    expect(screen.getByText("No priority suggestion")).toBeInTheDocument();
    view.unmount();
  });
  it("renders a safe non-blocking failure", async () => {
    analyze.mockRejectedValueOnce(new Error("provider secret"));
    render(<AiAnalysisCard ticketId="abc" />);
    await userEvent.click(
      screen.getByRole("button", { name: "Analyze Ticket" }),
    );
    expect(await screen.findByRole("alert")).toHaveTextContent(
      "ticket can still be managed",
    );
    expect(screen.getByRole("alert")).not.toHaveTextContent("provider secret");
  });
});
