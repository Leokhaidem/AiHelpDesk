import { useState, type FormEvent } from "react";
import { Loader2, Send } from "lucide-react";

import { FieldError } from "@/components/shared/field-error";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { formatDateTime } from "@/lib/dates";
import { isApiError } from "@/lib/error-mapping";

import { useAddComment, useTicketComments } from "../hooks";

import type { CommentResponse } from "../api";

const COMMENT_MAX = 2000;

function CommentItem({ comment }: { comment: CommentResponse }) {
  return (
    <li className="rounded-md bg-muted/40 p-3">
      <div className="flex items-baseline justify-between gap-2">
        <p className="text-sm font-medium">{comment.authorName ?? "Unknown"}</p>
        <p className="text-xs text-muted-foreground">{formatDateTime(comment.createdAt)}</p>
      </div>
      <p className="mt-1 text-sm whitespace-pre-wrap">{comment.content}</p>
    </li>
  );
}

/**
 * Shared comment thread for every ticket detail page (P6/P15/P20). The
 * backend enforces who may read and comment (employee: own tickets,
 * agent: assigned, admin: all).
 */
export function CommentThread({ ticketId }: { ticketId: number }) {
  const comments = useTicketComments(ticketId);
  const addComment = useAddComment();
  const [content, setContent] = useState("");
  const [touched, setTouched] = useState(false);

  const commentError =
    content.length > COMMENT_MAX
      ? `Comment must not exceed ${COMMENT_MAX} characters.`
      : undefined;

  const onSubmit = (event: FormEvent) => {
    event.preventDefault();
    setTouched(true);
    if (commentError || content.trim().length === 0 || addComment.isPending) {
      return;
    }
    addComment.mutate(
      { ticketId, content: content.trim() },
      {
        onSuccess: () => {
          setContent("");
          setTouched(false);
        },
      },
    );
  };

  return (
    <section className="rounded-lg border bg-card p-4" aria-label="Comments">
      <h2 className="mb-3 text-sm font-medium text-muted-foreground">
        Comments
        {comments.data ? (
          <span className="ml-1 text-muted-foreground/70 tabular-nums">({comments.data.length})</span>
        ) : null}
      </h2>

      {comments.isError ? (
        <Alert variant="destructive">
          <AlertDescription>
            {isApiError(comments.error)
              ? comments.error.detail
              : "Comments could not be loaded."}
          </AlertDescription>
        </Alert>
      ) : comments.data && comments.data.length > 0 ? (
        <ul className="space-y-3">
          {comments.data.map((comment) => (
            <CommentItem key={comment.id} comment={comment} />
          ))}
        </ul>
      ) : (
        <p className="text-sm text-muted-foreground">
          No comments yet — stakeholders of this ticket can add one below.
        </p>
      )}

      <form className="mt-4" onSubmit={onSubmit} noValidate>
        <Label htmlFor={`comment-${ticketId}`}>Add a comment</Label>
        <Textarea
          id={`comment-${ticketId}`}
          rows={3}
          className="mt-1.5"
          placeholder="Write a note for the other people working on this ticket"
          value={content}
          onChange={(event) => setContent(event.target.value)}
          onBlur={() => setTouched(true)}
          aria-invalid={commentError ? true : undefined}
        />
        <div className="mt-1 flex items-center justify-between gap-2">
          <FieldError message={touched ? commentError : undefined} />
          <span className="ml-auto text-xs text-muted-foreground tabular-nums">
            {content.length}/{COMMENT_MAX}
          </span>
        </div>
        <div className="mt-2 flex justify-end">
          <Button
            type="submit"
            size="sm"
            disabled={addComment.isPending || content.trim().length === 0 || Boolean(commentError)}
          >
            {addComment.isPending ? (
              <Loader2 className="animate-spin" aria-hidden="true" />
            ) : (
              <Send aria-hidden="true" />
            )}
            Post comment
          </Button>
        </div>
      </form>
    </section>
  );
}